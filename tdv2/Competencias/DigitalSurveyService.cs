using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;

namespace Tdv2.Competencias;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DigitalSave(int Version, JsonObject Profile, Dictionary<string, DigitalAnswer> Answers, Guid Generation = default);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DigitalFinalize(int Version, Guid Generation = default);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DigitalAccept(string Version, Guid Generation = default);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record DigitalTargetInput(int? Value, int Version, Guid Generation = default);
public sealed record DigitalSurveyView(int Version, DateTimeOffset? CompletedAt, JsonObject Institution, JsonObject Profile,
    Dictionary<string, DigitalAnswer> Answers, JsonObject Instrument, bool Accepted, DigitalEvaluation Evaluation, DigitalNavigation Navigation, Guid Generation = default);

public sealed class DigitalSurveyService(DigitalDbContext db, DigitalInstrument instrument, DatabaseConnections connections, AccessState state)
{
    private static JsonObject Object(string value) => JsonNode.Parse(value)!.AsObject();
    private static Dictionary<string, DigitalAnswer> Answers(string value) => JsonSerializer.Deserialize<Dictionary<string, DigitalAnswer>>(value, DigitalInstrument.Json)!;
    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, DigitalInstrument.Json);
    public async Task<DigitalSurveyView> Read(DigitalScope scope, CancellationToken ct, string? step = null)
    {
        await using var read = await db.Database.BeginTransactionAsync(ct);
        var generation = await DigitalGeneration.Shared(db, ct);
        var employee = UnitDirectory.Employee(scope.Profile.User)!;
        var survey = await db.Surveys.AsNoTracking().SingleOrDefaultAsync(s => s.Employee == employee, ct);
        var accepted = survey?.CompletedAt is not null || await db.Consents.AnyAsync(c => c.Employee == employee && c.NoticeVersion == instrument.NoticeVersion, ct);
        var selected = survey is null ? instrument : new DigitalInstrument(survey.InstrumentJson);
        var profile = survey is null ? new JsonObject() : Object(survey.ProfileJson);
        var answers = survey is null ? new() : Answers(survey.AnswersJson);
        var evaluation = selected.Evaluate(profile, answers);
        if (survey?.ReportJson is { } final) evaluation = evaluation with { Report = JsonSerializer.Deserialize<DigitalReport>(final, DigitalInstrument.Json)! };
        // En captura se muestra el contexto vigente; al finalizar se conserva la instantánea.
        var institution = survey?.CompletedAt is not null ? Object(survey.InstitutionJson) : JsonSerializer.SerializeToNode(scope.Institution(), DigitalInstrument.Json)!.AsObject();
        var resource = selected.Resource.DeepClone().AsObject(); resource["notice"] = instrument.Resource["notice"]!.DeepClone();
        return new(survey?.Version ?? 0, survey?.CompletedAt, institution, profile, answers, resource, accepted, evaluation,
            selected.Navigation(evaluation, institution["status"]?.ToString() == "available", step), generation);
    }
    private async Task<T> Write<T>(DigitalScope scope, Guid generation, Func<DigitalSurvey?, Task<T>> work, CancellationToken ct)
    {
        // Mantener SHARE sobre la sesión evita que representación/logout se intercale con el commit
        // de la base independiente. No se intenta una FK ni transacción distribuida entre bases.
        await using var shared = await connections.Open("Tdv2", ct);
        await using var session = await shared.BeginTransactionAsync(ct);
        await state.Guard(shared, session, ct);
        if (state.Loaded?.Selection is not null) throw new DomainProblem(403, "La representación permite únicamente consultas.");
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        DigitalGeneration.Require(await DigitalGeneration.Shared(db, ct), generation);
        await using (var guard = new NpgsqlCommand("SELECT pg_advisory_xact_lock(hashtextextended($1,27102026))", (NpgsqlConnection)db.Database.GetDbConnection(), (NpgsqlTransaction)transaction.GetDbTransaction()))
        { guard.Parameters.AddWithValue(UnitDirectory.Employee(scope.Profile.User)!); await guard.ExecuteNonQueryAsync(ct); }
        var survey = await db.Surveys.SingleOrDefaultAsync(s => s.Employee == UnitDirectory.Employee(scope.Profile.User), ct);
        var result = await work(survey);
        await db.SaveChangesAsync(ct);
        await state.Guard(shared, session, ct);
        await transaction.CommitAsync(ct);
        await session.CommitAsync(ct);
        return result;
    }
    public async Task Accept(DigitalScope scope, DigitalAccept input, CancellationToken ct)
    {
        if (input.Version != instrument.NoticeVersion) throw new DomainProblem(409, "El aviso cambió. Revisa la versión vigente.");
        await Write(scope, input.Generation, async survey =>
        {
            if (survey?.CompletedAt is not null) throw new DomainProblem(409, "La encuesta ya fue finalizada.");
            var person = scope.Profile.User; var employee = UnitDirectory.Employee(person)!;
            if (!await db.Consents.AnyAsync(c => c.Employee == employee && c.NoticeVersion == instrument.NoticeVersion, ct))
                db.Consents.Add(new() { Employee = employee, Email = person.Email, NoticeVersion = instrument.NoticeVersion, AcceptedAt = DateTimeOffset.UtcNow });
            if (survey is null)
            {
                var now = DateTimeOffset.UtcNow;
                db.Surveys.Add(new() { Employee = employee, Email = person.Email, Name = person.Name, UnitId = scope.Institution().Unit?.Id,
                    InstitutionJson = Serialize(scope.Institution()), InstrumentVersion = instrument.Version, InstrumentJson = instrument.Resource.ToJsonString(), CreatedAt = now, UpdatedAt = now });
            }
            return true;
        }, ct);
    }
    private async Task RequireConsent(DigitalScope scope, CancellationToken ct)
    {
        if (!await db.Consents.AnyAsync(c => c.Employee == UnitDirectory.Employee(scope.Profile.User) && c.NoticeVersion == instrument.NoticeVersion, ct))
            throw new DomainProblem(403, "Confirma el aviso de privacidad antes de guardar respuestas.");
    }
    private static void Mutable(DigitalSurvey? survey, int version)
    {
        if (survey is null) throw new DomainProblem(403, "Confirma el aviso de privacidad antes de comenzar.");
        if (survey.CompletedAt is not null) throw new DomainProblem(409, "La encuesta ya fue finalizada y no admite cambios.");
        if (survey.Version != version) throw new DomainProblem(409, "La encuesta cambió en otra pestaña o sesión. Recarga para continuar.");
    }
    public async Task Save(DigitalScope scope, DigitalSave input, CancellationToken ct)
    {
        await Write(scope, input.Generation, async survey =>
        {
            // Respuesta de red perdida después del commit: el reintento exacto no crea otra
            // revisión ni sobrescribe cambios. Un documento distinto sigue siendo conflicto.
            if (survey is { CompletedAt: null } && survey.Version > input.Version && input.Profile is not null && input.Answers is not null
                && JsonNode.DeepEquals(Object(survey.ProfileJson), input.Profile))
            {
                var previous = Answers(survey.AnswersJson);
                if (previous.Count == input.Answers.Count && previous.All(p => input.Answers.TryGetValue(p.Key, out var answer) && p.Value == answer))
                { await RequireConsent(scope, ct); return true; }
            }
            Mutable(survey, input.Version); await RequireConsent(scope, ct);
            if (input.Profile is null || input.Answers is null) throw new DomainProblem(422, "Completa los datos de la solicitud.");
            new DigitalInstrument(survey!.InstrumentJson).ValidateSectionChanges(Object(survey.ProfileJson), Answers(survey.AnswersJson),
                input.Profile, input.Answers, scope.Institution().Status == "available");
            survey.ProfileJson = input.Profile.ToJsonString(); survey.AnswersJson = Serialize(input.Answers);
            survey.InstitutionJson = Serialize(scope.Institution()); survey.UnitId = scope.Institution().Unit?.Id;
            survey.Email = scope.Profile.User.Email; survey.Name = scope.Profile.User.Name;
            survey.UpdatedAt = DateTimeOffset.UtcNow; survey.Version++;
            return true;
        }, ct);
    }
    public async Task Finalize(DigitalScope scope, DigitalFinalize input, CancellationToken ct)
    {
        await Write(scope, input.Generation, async survey =>
        {
            // Reintento de finalización: resultado definitivo idéntico, incluso con la versión anterior.
            if (survey?.CompletedAt is not null) return true;
            Mutable(survey, input.Version); await RequireConsent(scope, ct);
            var institution = scope.Institution();
            if (institution.Status != "available") throw new DomainProblem(422, "La adscripción institucional está incompleta o es inconsistente. Solicita su revisión antes de finalizar.");
            var evaluation = new DigitalInstrument(survey!.InstrumentJson).Evaluate(Object(survey.ProfileJson), Answers(survey.AnswersJson));
            if (evaluation.Pending.Length > 0) throw new DomainProblem(422, "Completa todas las preguntas aplicables antes de finalizar.", new { pending = evaluation.Pending });
            var frozenInstitution = JsonSerializer.SerializeToNode(institution, DigitalInstrument.Json)!.AsObject();
            frozenInstitution["noticeVersion"] = instrument.NoticeVersion;
            survey.InstitutionJson = frozenInstitution.ToJsonString(); survey.UnitId = institution.Unit!.Id;
            survey.Email = scope.Profile.User.Email; survey.Name = scope.Profile.User.Name;
            survey.ReportJson = Serialize(evaluation.Report); survey.CompletedAt = DateTimeOffset.UtcNow;
            survey.UpdatedAt = survey.CompletedAt.Value; survey.Version++;
            return true;
        }, ct);
    }
    public async Task Target(DigitalScope scope, string unit, DigitalTargetInput input, CancellationToken ct)
    {
        if (!scope.Profile.Has("administrador") || !scope.Module(4)) throw new DomainProblem(403, "Solo el administrador de esta aplicación puede editar metas.");
        scope.Filter(unit);
        if (input.Value is < 0 or > 1000000) throw new DomainProblem(422, "La meta debe ser un número entre 0 y 1000000.");
        await Write(scope, input.Generation, async _ =>
        {
            // Serialización de metas entre administradores distintos.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({unit},27102027))", ct);
            var target = await db.Targets.SingleOrDefaultAsync(t => t.UnitId == unit, ct);
            if ((target?.Version ?? 0) != input.Version) throw new DomainProblem(409, "La meta cambió. Recarga antes de guardar.");
            if (input.Value is null) { if (target is not null) db.Targets.Remove(target); }
            else
            {
                if (target is null) { target = new() { UnitId = unit, Version = 0 }; db.Targets.Add(target); }
                target.Value = input.Value.Value; target.Version++; target.UpdatedAt = DateTimeOffset.UtcNow; target.UpdatedBy = scope.Profile.User.Email;
            }
            return true;
        }, ct);
    }
}
