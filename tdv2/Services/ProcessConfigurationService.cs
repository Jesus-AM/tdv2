using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
namespace Tdv2.Services;

public sealed record ParticipationChange(int Version, int[] Levels, string[] ExcludedTypes, string? Confirmation = null);

/// <summary>La vista previa no guarda. La confirmación vuelve a calcular su huella bajo bloqueos breves.</summary>
public sealed class ProcessConfigurationService(Tdv2DbContext db, IFormStore store, AccessState state,
    OperationAudit audit, IDataProtectionProvider protection, FormNotifications notifications)
{
    private readonly IDataProtector protector = protection.CreateProtector("TDV2.ParticipationPreview.v1");
    public async Task<Dictionary<string, object?>> Read(CancellationToken ct)
    {
        var policy = await store.Participation(ct);
        var units = await store.Units(ct);
        return new()
        {
            ["settings"] = new { version = policy.Version, levels = policy.Levels, excludedTypes = policy.ExcludedTypes },
            ["levels"] = units.Where(u => u.LevelKnown).Select(u => u.Level).Concat(policy.Levels).Distinct().Order().ToArray(),
            ["types"] = units.Select(u => Participation.Normalize(u.Kind)).Concat(policy.ExcludedTypes).Distinct().Order(StringComparer.Ordinal).ToArray()
        };
    }
    public async Task<object> PreviewOrSave(ParticipationChange input, bool save, CancellationToken ct)
    {
        if (input.Levels is null || input.ExcludedTypes is null || input.Levels.Length > 100
            || input.Levels.Any(n => n < 0 || n > 99) || input.ExcludedTypes.Length > 100
            || input.ExcludedTypes.Any(t => t is null || t.Length > 32)) throw new DomainProblem(422, "Revisa los niveles y tipos seleccionados.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        var transaction = (NpgsqlTransaction)tx.GetDbTransaction();
        await state.Guard(connection, transaction, ct);
        // Exclusivo antes de leer catálogo y formatos. Las capturas y publicaciones toman FOR SHARE
        // en el mismo orden: la vista previa no puede confirmarse sobre una instantánea ya obsoleta.
        await db.Database.ExecuteSqlRawAsync("SELECT version FROM configuracion_procesos WHERE id=1 FOR UPDATE", ct);
        var current = await db.ProcessSettings.SingleAsync(ct);
        if (input.Version != current.Version) throw new DomainProblem(409, "La configuración cambió. Recarga y vuelve a revisar el impacto.");
        var units = await store.Units(ct);
        var levels = input.Levels.Distinct().Order().ToArray();
        var types = input.ExcludedTypes.Select(Participation.Normalize).Distinct().Order(StringComparer.Ordinal).ToArray();
        if (levels.Except(units.Where(u => u.LevelKnown).Select(u => u.Level).Concat(current.Levels)).Any()
            || types.Except(units.Select(u => Participation.Normalize(u.Kind)).Concat(current.ExcludedTypes)).Any())
            throw new DomainProblem(422, "Selecciona opciones del catálogo o de la configuración guardada.");
        var before = new UnitDirectory(units, new(current.Version, current.Levels, current.ExcludedTypes));
        var after = new UnitDirectory(units, new(current.Version + 1, levels, types));
        var entering = after.Units.Values.Where(u => after.Participates(u) && !before.Participates(u)).OrderBy(u => u.Id).ToArray();
        var leaving = before.Units.Values.Where(u => before.Participates(u) && !after.Participates(u)).OrderBy(u => u.Id).ToArray();
        var affected = entering.Concat(leaving).Select(u => u.Id).ToArray();
        var forms = await db.UnitForms.AsNoTracking().Where(f => affected.Contains(f.UnitId))
            .OrderBy(f => f.UnitId).Select(f => new { id = f.UnitId, version = f.Version, enviadoEn = f.SubmittedAt }).ToArrayAsync(ct);
        // Describe alcances potenciales por adscripción, sin enumerar personas ni inferir concesiones de sus roles.
        var branches = new Dictionary<(UnitDirectory Directory, string Root), string[]>();
        string[] Branch(UnitDirectory d, Unit u)
        {
            if (d.LevelTwo(u.Id) is not { } root) return [];
            if (!branches.TryGetValue((d, root.Id), out var result))
                branches[(d, root.Id)] = result = d.Units.Values.Where(x => d.Participates(x) && d.LevelTwo(x.Id)?.Id == root.Id).Select(x => x.Id).Order().ToArray();
            return result;
        }
        var scopeChanges = before.Units.Values.OrderBy(u => u.Id).Select(u => new
        {
            unidad = u.Public(), localAntes = before.FormUnit(u.Id)?.Id, localDespues = after.FormUnit(u.Id)?.Id,
            dependenciasAntes = Branch(before, u), dependenciasDespues = Branch(after, u)
        }).Where(x => x.localAntes != x.localDespues || !x.dependenciasAntes.SequenceEqual(x.dependenciasDespues)).ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        { version = current.Version, levels, types, catalog = units.OrderBy(u => u.Id), forms }))));
        if (!save)
        {
            await tx.CommitAsync(ct);
            return new { confirmation = protector.Protect(hash), entering = entering.Select(u => u.Public()),
                leaving = leaving.Select(u => u.Public()), forms, scopeChanges };
        }
        string? confirmed = null;
        try { if (input.Confirmation is not null) confirmed = protector.Unprotect(input.Confirmation); }
        catch (CryptographicException) { /* Sólo se aceptan confirmaciones emitidas por este servidor. */ }
        if (confirmed != hash) throw new DomainProblem(409, "Cambió el catálogo, la configuración o un formato afectado. Revisa una nueva vista previa antes de guardar.");
        var old = new { version = current.Version, levels = current.Levels, excludedTypes = current.ExcludedTypes };
        current.Levels = levels; current.ExcludedTypes = types; current.Version++; current.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.Write(connection, transaction, "participacion_actualizada", "configuracion_procesos", "1", "guardado", ct,
            details: new { anterior = old, nuevo = new { version = current.Version, levels, excludedTypes = types }, areasEntrantes = entering.Select(u => u.Id), areasSalientes = leaving.Select(u => u.Id) });
        await tx.CommitAsync(ct);
        notifications.ParticipationChanged();
        return new { message = "Participación guardada.", version = current.Version };
    }
}
