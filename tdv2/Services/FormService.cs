using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
using Tdv2.Synchronization;
using Tdv2.Web;
namespace Tdv2.Services;

/// <summary>Coordina la consulta y captura de formatos por UR dentro de una solicitud autorizada.</summary>
public sealed class FormService(RequestAccess access, IFormStore store, FormSchema schema, ILocalCatalog catalogs, IHttpContextAccessor accessor)
{
    // RequestAccess es scoped: la identidad y Nexo se revalidan en cada solicitud, nunca en un singleton.
    private HttpContext http => accessor.HttpContext ?? throw new InvalidOperationException("No hay una solicitud activa.");

    public async Task<PageData> Index()
    {
        var context = await access.Resolve(http);
        var rows = new List<Dictionary<string, object?>>();
        var records = await store.GetMany(context.Scopes.Keys.ToArray(), http.RequestAborted);
        var inventories = await catalogs.Inventories(context.Scopes.Values.Where(s => records.GetValueOrDefault(s.Unit.Id)?.SubmittedAt is null
            && records.GetValueOrDefault(s.Unit.Id)?.Stage?.Blocked != true
            && FormCapture.CurrentYear(records.GetValueOrDefault(s.Unit.Id)?.Year, s.Unit)).Select(s => s.Unit).ToArray(), http.RequestAborted);
        foreach (var scope in context.Scopes.Values.OrderBy(s => s.Unit.Code, StringComparer.OrdinalIgnoreCase))
        {
            var record = records.GetValueOrDefault(scope.Unit.Id);
            var current = record?.SubmittedAt is null && record?.Stage?.Blocked != true && FormCapture.CurrentYear(record?.Year, scope.Unit);
            var progress = record?.Progress ?? 0;
            if (current)
            {
                // Recalcular sólo la proyección de consulta; un GET no modifica borradores ni instantáneas enviadas.
                var draft = record?.Content.DeepClone().AsObject() ?? schema.Blank(scope.Unit);
                if ((record?.Stage?.id ?? FormStages.First) == FormStages.First) LocalCatalog.Merge(draft, inventories[scope.Unit.Id]);
                progress = FormStages.Progress(record?.Stage?.id ?? FormStages.First, draft);
            }
            rows.Add(new()
            {
                ["id_ur"] = scope.Unit.Id,
                ["cve_ur"] = scope.Unit.Code,
                ["desc_ur"] = scope.Unit.Description ?? scope.Unit.Code,
                ["nivel_ur"] = scope.Unit.Level,
                ["id_ur_pertenece"] = scope.Unit.Parent,
                ["id_ur_principal"] = context.Directory.DirectoryParent(scope.Unit),
                ["tipo_ur"] = scope.Unit.Kind,
                ["propia"] = scope.Own,
                ["ejercicio"] = scope.Unit.Year,
                ["editable"] = scope.Edit && !context.ReadOnly && current,
                ["enviado_en"] = record?.SubmittedAt,
                ["porcentaje"] = progress,
                ["actualizado_en"] = record?.UpdatedAt,
                ["actualizado_por"] = record?.UpdatedBy,
                ["url"] = "/formatos/" + Uri.EscapeDataString(scope.Unit.Id)
            });
        }
        var admin = context.Profile.Has("administrador");
        return new PageData("Inicio", new()
        {
            ["formatos"] = rows,
            ["participacion"] = new { levels = context.Directory.Participation.Levels, excludedTypes = context.Directory.Participation.ExcludedTypes },
            ["sinFormatosMotivo"] = rows.Count == 0 ? new FormAccess(context.Directory).NoScopeReason(context.Profile) : null,
            ["consultaInstitucional"] = context.Profile.InstitutionalRead,
            ["administrador"] = admin,
            ["directorio"] = context.Profile.InstitutionalRead ? rows : [],
            ["urAdministracion"] = admin ? context.Directory.AdministratorRoot(context.Profile.User)?.Public() : null,
            ["puedeColaboradores"] = admin || context.Profile.Responsible && (access.Selection is { Kind: "preview" } preview
                ? context.Directory.Get(preview.UnitId) is { } selected && UnitDirectory.IsForm(selected)
                : context.Directory.Responsibilities(context.Profile.User).Any(UnitDirectory.IsForm)),
            ["sincronizadoEn"] = await catalogs.LastSii(http.RequestAborted)
        }, context.Profile);
    }

    public async Task<PageData> Show(string ur)
    {
        var context = await access.Resolve(http);
        if (!context.Scopes.TryGetValue(ur, out var scope)) throw new DomainProblem(403, "No tienes acceso al formato de esta UR.");
        var record = await store.Get(ur, http.RequestAborted);
        var current = record?.SubmittedAt is null && record?.Stage?.Blocked != true && FormCapture.CurrentYear(record?.Year, scope.Unit);
        var content = record?.Content.DeepClone() ?? schema.Blank(scope.Unit);
        object inventory = current && (record?.Stage?.id ?? FormStages.First) == FormStages.First
            ? LocalCatalog.Merge(content.AsObject(), await catalogs.Inventory(scope.Unit, http.RequestAborted)).Status
            : new { estado = "enviado", nuevos = 0, total = content["identificacion"]!.AsArray().Count, aviso = (string?)null };
        var snapshot = record?.SubmissionSnapshot is { } frozen ? JsonNode.Parse(frozen) : null;
        var stage = record?.Stage ?? new FormStageStatus(FormStages.First, FormStages.Name(FormStages.First), true,
            record?.SubmittedAt is not null ? "historico" : "borrador", record?.Year is > 0 ? record.Year : scope.Unit.Year,
            record?.SubmittedAt, record?.SubmittedEffective, null);
        return new PageData("FormatoUR", new()
        {
            ["unidad"] = (object?)snapshot?["unidad"] ?? scope.Unit.Public(),
            ["contenido"] = content,
            ["entrega"] = FormStages.Describe(stage, content.AsObject()),
            ["plantilla"] = schema.Blank(scope.Unit),
            ["definicion"] = schema.Definition(),
            ["editable"] = scope.Edit && !context.ReadOnly && current,
            ["seccionesPosteriores"] = FormCapture.LaterSections(context.Profile),
            ["porcentajeEtapa"] = FormStages.Progress(stage.id, content.AsObject()),
            ["puedeEnviar"] = scope.Edit && !context.ReadOnly && current && new FormAccess(context.Directory).CanSubmit(context.Profile, scope.Unit),
            ["enviadoEn"] = record?.SubmittedAt,
            ["enviadoPor"] = record?.SubmittedEffective,
            ["permisoEdicion"] = scope.Edit,
            ["version"] = record?.Version ?? 0,
            ["porcentaje"] = !current ? record!.Progress : FormStages.Progress(stage.id, content.AsObject()),
            ["actualizadoEn"] = record?.UpdatedAt,
            ["actualizadoPor"] = record?.UpdatedBy,
            ["guardarUrl"] = "/formatos/" + Uri.EscapeDataString(ur),
            ["ilda"] = inventory
        }, context.Profile);
    }

    public async Task<LocalSiiModules> Modules(string ur)
    {
        var context = await access.Resolve(http);
        if (!context.Scopes.ContainsKey(ur)) throw new DomainProblem(403, "No tienes acceso al formato de esta UR.");
        return await catalogs.SiiModules(http.RequestAborted);
    }

}
