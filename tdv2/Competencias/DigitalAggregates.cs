using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Integrations.Nexo;
using Tdv2.Infrastructure;

namespace Tdv2.Competencias;

public sealed record DigitalRegisteredCounts(int Stored, int Completed, int InProgress, int WithoutCapture);
public sealed record DigitalSummary(int? Completed, int? InProgress, int? ParticipatingUnits, int? Target, int ConfiguredTargets, int TotalUnits, double? Participation, double? Maturity, int? NotStarted = null, bool PopulationAvailable = true, string? PopulationProblem = null, DigitalRegisteredCounts? Registered = null);
public sealed record DigitalUnitSummary(string Id, string Code, string Name, int Level, string? Parent, string? ParentName, int DirectCompleted, int DirectInProgress, DigitalSummary Summary);
public sealed record DigitalDistribution(string Id, string Name, int Count, int NotApplicable, int NotUsed, int[] Levels, double? Score);
public sealed record DigitalAggregatesResult(string? SelectedUnit, int? SelectedLevel, bool Institutional, DigitalUnitOption[] Roots, DigitalUnitOption[] Options, int[] Levels,
    DigitalSummary Summary, DigitalUnitSummary[] Units, DigitalDistribution[] Blocks, DigitalDistribution[] Tools, DigitalDistribution[] Positions,
    int[] CompetenceLevels, DigitalRecommendation[] Recommendations, string? SelectedPosition, string[] PositionOptions);

public sealed class DigitalAggregates(DigitalDbContext db, DigitalInstrument instrument, IDigitalNexo nexo, DatabaseConnections connections)
{
    public static bool Selectable(Unit unit) => unit.LevelKnown && unit.Level > 1 && UnitDirectory.IsEligible(unit);
    // El aviso crea una fila vacía: ni esa fila ni el perfil institucional constituyen inicio.
    public static bool Started(string profile, string answers)
        => JsonNode.Parse(profile)!.AsObject().Any(p => p.Value is JsonArray a ? a.Count > 0 : !string.IsNullOrWhiteSpace(p.Value?.ToString()))
            || JsonSerializer.Deserialize<Dictionary<string, DigitalAnswer>>(answers, DigitalInstrument.Json)!.Values.Any(a => a.State is "level" or "na" or "unused");
    public async Task<DigitalAggregatesResult> Read(DigitalScope scope, string? unit, int? level, CancellationToken ct, string? position = null, bool finalizedOnly = false)
    {
        await using var read = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, ct);
        await DigitalGeneration.Shared(db, ct);
        var ids = scope.Filter(unit); // Autorización antes de leer población o resultados.
        var allowed = scope.Allowed;
        var selectable = allowed.Where(Selectable).ToArray();
        int[] availableLevels = scope.Institutional ? [2, 3, 4, 5] : [];
        if (scope.Institutional) level ??= 2;
        if (unit is not null && !selectable.Any(u => u.Id == unit)) throw new DomainProblem(422, "Esta unidad no está disponible como filtro.");
        if (level is not null && !availableLevels.Contains(level.Value))
            throw new DomainProblem(403, "El nivel no está disponible en este contexto.");
        if (level is not null)
        {
            // El tipo N se oculta en el selector de unidades, no en la agrupación por nivel.
            var groups = allowed.Where(u => u.Level == level && (unit is null || scope.Directory.Within(u.Id, unit) || scope.Directory.Within(unit, u.Id))).Select(u => u.Id).ToArray();
            ids = ids.Where(id => groups.Any(root => scope.Directory.Within(id, root))).ToArray();
        }
        var positionsAvailable = instrument.Resource["profile"]!.AsArray().Single(p => p!["id"]!.ToString() == "puesto")!["options"]!.AsArray().Select(p => p!.ToString()).ToArray();
        if (!finalizedOnly && position is not null) throw new DomainProblem(422, "La participación incluye todos los perfiles, también a quienes aún no han respondido.");
        NexoPopulationPerson[] population = []; var available = true; string? populationProblem = null;
        if (!finalizedOnly)
        {
            try { population = await nexo.Population(ids, scope.Directory.Units.Values.Where(u => scope.Chain(u.Id) is not null).Select(u => u.Id).ToArray(), ct); }
            catch (DomainProblem e) when (e.Status is 403 or 503)
            { available = false; populationProblem = e.Details is NexoPopulationProblem { Kind: "integrity" } ? "integrity" : "source"; }
        }
        population = population.DistinctBy(p=>p.UserId).ToArray();
        var employees = population.Select(p => p.Employee).OfType<string>().Distinct().ToArray();
        var currentUnits = population.ToDictionary(p => p.UserId, p => p.UnitId);
        // Participación actual: adscripción publicada hoy. Resultados históricos: contexto congelado al finalizar.
        var query = finalizedOnly
            ? db.Surveys.AsNoTracking().Where(s => s.CompletedAt != null && s.UnitId != null && ids.Contains(s.UnitId))
            : db.Surveys.AsNoTracking().Where(s => employees.Contains(s.Employee));
        // Último definitivo por persona: el envío archivado sigue consultable al reabrir,
        // sin sumarlo a otro envío ni usarlo como finalización vigente en Participación.
        var archived = finalizedOnly ? (await db.Submissions.AsNoTracking().Where(h => h.UnitId != null && ids.Contains(h.UnitId)
            && !db.Surveys.Any(s => s.Id == h.SurveyId && s.CompletedAt != null)
            && !db.Submissions.Any(n => n.SurveyId == h.SurveyId && n.Revision > h.Revision))
            .Select(h => h.SnapshotJson).ToArrayAsync(ct)).Select(DigitalSurveyService.RestoreSnapshot).ToArray() : [];
        // Los perfiles históricos sólo se ofrecen si existen dentro del alcance autorizado.
        var historicalProfiles = finalizedOnly ? await query.Select(s => s.ProfileJson).Distinct().ToListAsync(ct) : [];
        positionsAvailable = positionsAvailable.Concat(historicalProfiles.Concat(archived.Select(s => s.ProfileJson)).Select(p => JsonNode.Parse(p)?["puesto"]?.ToString()).OfType<string>()).Distinct().ToArray();
        if (position is not null && !positionsAvailable.Contains(position)) throw new DomainProblem(422, "El perfil laboral no está disponible en este conjunto.");
        if (position is not null)
        {
            var filter = JsonSerializer.Serialize(new { puesto = position });
            query = query.Where(s => EF.Functions.JsonContains(s.ProfileJson, filter));
        }
        var surveys = await query
            .Select(s => new { s.Employee, s.UnitId, s.CompletedAt, s.ReportJson, s.ProfileJson, s.AnswersJson, s.Email, s.CreatedAt }).ToListAsync(ct);
        surveys.AddRange(archived.Where(s => position is null || JsonNode.Parse(s.ProfileJson)?["puesto"]?.ToString() == position)
            .Select(s => new { s.Employee, s.UnitId, s.CompletedAt, s.ReportJson, s.ProfileJson, s.AnswersJson, s.Email, s.CreatedAt }));
        var correspondence = finalizedOnly || !available ? new DigitalCorrespondence([],[]) : await DigitalSurveyCorrespondence.Resolve(connections,population,
            surveys.Select(s=>new DigitalSurveyReference(s.Employee,s.Email,s.CreatedAt,s.CompletedAt is not null || Started(s.ProfileJson,s.AnswersJson),s.CompletedAt is not null)).ToArray(),ct);
        // Conteos registrados independientes, limitados en SQL a las UR guardadas.
        // Sólo se consultan si hay un fallo de población/correspondencia.
        var registered = !finalizedOnly && (!available || correspondence.Pending.Count>0)
            ? await db.Surveys.AsNoTracking().Where(s => s.UnitId != null && ids.Contains(s.UnitId))
                .Select(s => new { s.Employee, s.UnitId, s.CompletedAt, s.ProfileJson, s.AnswersJson }).ToArrayAsync(ct) : [];
        var registeredStarted = registered.Where(s => s.CompletedAt is not null || Started(s.ProfileJson,s.AnswersJson))
            .Select(s=>s.Employee).ToHashSet(StringComparer.Ordinal);
        if(!finalizedOnly) surveys.RemoveAll(s=>!correspondence.Owners.ContainsKey(s.Employee));
        // Un participante tiene una sola fila. No se suman los resúmenes de padres e hijos.
        var reports = surveys.Where(s => s.CompletedAt is not null).ToDictionary(s => s.Employee, s => JsonSerializer.Deserialize<DigitalReport>(s.ReportJson!, DigitalInstrument.Json)!);
        var startedEmployees = surveys.Where(s => s.CompletedAt is not null || Started(s.ProfileJson, s.AnswersJson)).Select(s => s.Employee).ToHashSet(StringComparer.Ordinal);
        DigitalSummary Summarize(string[] set)
        {
            if (!available)
            {
                var stored = registered.Where(s => set.Contains(s.UnitId)).DistinctBy(s => s.Employee).ToArray();
                var doneStored = stored.Count(s => s.CompletedAt is not null);
                var startedStored = stored.Count(s => registeredStarted.Contains(s.Employee));
                return new(null, null, null, null, 0, set.Length, null, null, null, false, populationProblem,
                    new(stored.Length, doneStored, startedStored - doneStored, stored.Length - startedStored));
            }
            var members = population.Where(p => set.Contains(p.UnitId)).DistinctBy(p=>p.UserId).ToArray();
            var memberIds = members.Select(p => p.UserId).ToHashSet();
            var unresolved = correspondence.Pending.Where(p=>memberIds.Contains(p.Key)).Select(p=>p.Value).ToArray();
            var people = surveys.Where(s => finalizedOnly ? set.Contains(s.UnitId) : correspondence.Owners.TryGetValue(s.Employee,out var owner) && memberIds.Contains(owner)).DistinctBy(s => s.Employee).ToArray();
            var done = people.Count(s => s.CompletedAt is not null);
            var started = people.Where(s => startedEmployees.Contains(s.Employee)).ToArray();
            int? target = finalizedOnly ? null : members.Length;
            int? final = unresolved.Any(p=>p.Finalized) ? null : done;
            var registeredSet = registered.Where(s=>set.Contains(s.UnitId)).DistinctBy(s=>s.Employee).ToArray();
            var registeredDone = registeredSet.Count(s=>s.CompletedAt is not null);
            var registeredStartedCount = registeredSet.Count(s=>registeredStarted.Contains(s.Employee));
            return new(final, unresolved.Any(p=>p.InProgress) ? null : started.Length - done,
                unresolved.Length>0 ? null : started.Select(p => finalizedOnly ? p.UnitId : currentUnits[correspondence.Owners[p.Employee]]).Distinct().Count(), target, 0, set.Length,
                target > 0 && final is not null ? final * 100d / target : null,
                unresolved.Any(p=>p.Finalized) ? null : DigitalInstrument.Mean(people.Where(p => p.CompletedAt is not null).Select(p => reports[p.Employee].Global)),
                unresolved.Any(p=>p.NotStarted) ? null : target - started.Length, true, unresolved.Length>0 ? "correspondence" : null,
                unresolved.Length>0 ? new(registeredSet.Length,registeredDone,registeredStartedCount-registeredDone,registeredSet.Length-registeredStartedCount) : null);
        }
        // Ocultar opciones no elimina los nodos de los resúmenes ni su personal adscrito.
        var rowUnits = allowed.Where(u => ids.Contains(u.Id) && (level is null || u.Level == level)).OrderBy(u => u.Code).ToArray();
        var rows = rowUnits.Select(u =>
        {
            var direct = surveys.Where(s => startedEmployees.Contains(s.Employee) && (finalizedOnly ? s.UnitId == u.Id : correspondence.Owners.TryGetValue(s.Employee,out var owner) && currentUnits[owner] == u.Id)).ToArray();
            var visibleParent = scope.Chain(u.Id)?.Skip(1).FirstOrDefault(p => rowUnits.Any(r => r.Id == p.Id))?.Id;
            return new DigitalUnitSummary(u.Id, u.Code, u.Description ?? u.Code, u.Level, visibleParent, scope.Directory.Get(u.Parent)?.Description,
                direct.Count(s => s.CompletedAt is not null), direct.Count(s => s.CompletedAt is null), Summarize(ids.Where(id => scope.Directory.Within(id, u.Id)).ToArray()));
        }).ToArray();
        var completed = reports.Values.ToArray();
        static DigitalDistribution Distribution(string id, string name, IEnumerable<double?> scores)
        {
            var values = scores.ToArray(); var levels = new int[4];
            foreach (var score in values) if (DigitalInstrument.ScoreLevel(score) is { } l) levels[l]++;
            return new(id, name, values.Count(v => v is not null), values.Count(v => v is null), 0, levels, DigitalInstrument.Mean(values));
        }
        var blocks = instrument.Blocks.Select(b => Distribution(b.Id, b.N, completed.Select(r => r.Blocks.SingleOrDefault(x => x.Id == b.Id)?.Score))).ToArray();
        var toolNames = instrument.Blocks.SelectMany(b => b.Tools).Select(t => new { t.Id, Name = t.N })
            .Concat(completed.SelectMany(r => r.Tools).Select(t => new { t.Id, t.Name })).DistinctBy(t => t.Id);
        var tools = toolNames.Select(t =>
        {
            var values = completed.SelectMany(r => r.Tools.Where(v => v.Id == t.Id)).ToArray(); var levels = new int[4];
            foreach (var v in values) if (v.Value >= 0) levels[v.Value.Value]++;
            return new DigitalDistribution(t.Id, t.Name, levels.Sum(), values.Count(v => v.Value == -1), values.Count(v => v.Value == -2), levels,
                DigitalInstrument.Mean(values.Where(v => v.Value >= 0).Select(v => (double?)(v.Value!.Value / 3d * 100))));
        }).ToArray();
        var positions = surveys.Where(s => s.CompletedAt is not null).GroupBy(s => JsonNode.Parse(s.ProfileJson)!["puesto"]!.ToString())
            .Select(g => Distribution(g.Key, g.Key, g.Select(s => reports[s.Employee].Global))).ToArray();
        var global = Distribution("global", "Global", completed.Select(r => r.Global));
        var recommendations = new List<DigitalRecommendation>();
        if (completed.Length > 0)
        {
            var low = blocks.Where(b => b.Score is not null).OrderBy(b => b.Score).FirstOrDefault();
            if (low is not null) recommendations.Add(new("warning", "Priorizar el bloque de " + low.Name, $"{low.Levels[0] + low.Levels[1]} de {low.Count} personas evaluadas están en nivel Inicial o Básico. Abrir primero cursos básicos y dar seguimiento por dependencia."));
            var cyberIds = instrument.Blocks.Single(b => b.Id == "ciber").Tools.Select(t => t.Id).ToArray();
            var cyber = tools.Where(t => cyberIds.Contains(t.Id) && t.Count > 0).OrderByDescending(t => (double)t.Levels[0] / t.Count).Take(3).ToArray();
            if (cyber.Length > 0) recommendations.Add(new("warning", "Cerrar riesgos básicos de ciberseguridad", "Temas con mayor proporción en Inicial: " + string.Join(", ", cyber.Select(t => t.Name)) + ". Campaña breve de MFA, phishing y reporte de incidentes con taller práctico."));
            var iaIds = instrument.Blocks.Single(b => b.Id == "ia").Tools.Select(t => t.Id).ToArray();
            var gaps = tools.Where(t => !iaIds.Contains(t.Id) && t.Count >= Math.Max(1, completed.Length * .3)).OrderBy(t => t.Score).Take(5).ToArray();
            if (gaps.Length > 0) recommendations.Add(new("info", "Herramientas institucionales con mayor brecha", string.Join(", ", gaps.Select(t => t.Name)) + ". Verifica con la DGTI las licencias necesarias antes de programar cursos."));
            var foundations = tools.Where(t => iaIds.Take(4).Contains(t.Id) && t.Count > 0).ToArray();
            if (foundations.Length > 0) recommendations.Add(new("info", "Uso responsable de la IA antes que más herramientas", $"En promedio {foundations.Average(t => t.Levels[0] * 100d / t.Count):F1} % está en Inicial en los fundamentos. Difundir el Manual práctico de IA y el curso Uso responsable de la inteligencia artificial generativa."));
            var weakest = rows.Where(r => r.Summary.Maturity is not null).OrderBy(r => r.Summary.Maturity).FirstOrDefault();
            if (weakest is not null && rows.Count(r => r.Summary.Maturity is not null) > 1) recommendations.Add(new("info", $"Acompañamiento focalizado en {weakest.Code} - {weakest.Name}", "Planear capacitación por dependencia con metas trimestrales y un enlace de transformación digital."));
            recommendations.Add(new("success", "Formar una red de agentes de cambio", $"{global.Levels[3]} personas alcanzan nivel Avanzado global. Invitar a mentorías y compartir prácticas, plantillas y flujos."));
            recommendations.Add(new("info", "Dar seguimiento a la capacitación", "Revisar el avance de la ruta y los programas por dependencia. Esta encuesta admite una sola respuesta definitiva."));
        }
        var allowedIds = allowed.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);
        DigitalUnitOption Option(Unit u) => DigitalUnitOption.From(u) with {
            Ancestors = scope.Chain(u.Id)?.Skip(1).Select(p => p.Id).Where(allowedIds.Contains).ToArray(),
            ParentName = scope.Directory.Get(u.Parent) is { } parent ? parent.Code + " - " + (parent.Description ?? parent.Code) : null };
        return new(unit, level, scope.Institutional, scope.Roots.Select(Option).ToArray(), selectable.Select(Option).ToArray(), availableLevels,
            Summarize(ids), rows, blocks, tools, positions, global.Levels, recommendations.ToArray(), position, positionsAvailable);
    }
    public static byte[] Csv(DigitalAggregatesResult data)
    {
        static string Cell(object? value)
        {
            var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            if (text.StartsWith('=') || text.StartsWith('+') || text.StartsWith('-') || text.StartsWith('@')) text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        var text = new StringBuilder("Tipo,Clave,Nombre,Finalizadas,En curso,Meta,Participación,Índice,Inicial,Básico,Intermedio,Avanzado,No aplica,No utiliza\r\n");
        void Row(params object?[] cells) => text.AppendLine(string.Join(',', cells.Select(Cell)));
        Row("Conjunto", data.SelectedUnit, "Participantes únicos" + (data.SelectedPosition is null ? "" : " · " + data.SelectedPosition), data.Summary.Completed, data.Summary.InProgress, data.Summary.Target, data.Summary.Participation, data.Summary.Maturity,
            data.CompetenceLevels[0], data.CompetenceLevels[1], data.CompetenceLevels[2], data.CompetenceLevels[3]);
        foreach (var u in data.Units) Row("UR y dependientes", u.Code, u.Name, u.Summary.Completed, u.Summary.InProgress, u.Summary.Target, u.Summary.Participation, u.Summary.Maturity);
        foreach (var (kind, rows) in new[] { ("Bloque", data.Blocks), ("Herramienta o tema", data.Tools), ("Tipo de puesto", data.Positions) })
            foreach (var r in rows) Row(kind, r.Id, r.Name, r.Count, null, null, null, r.Score, r.Levels[0], r.Levels[1], r.Levels[2], r.Levels[3], r.NotApplicable, r.NotUsed);
        foreach (var r in data.Recommendations) Row("Recomendación", "", r.Title + ": " + r.Text);
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }
}
