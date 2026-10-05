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
        foreach (var scope in context.Scopes.Values.OrderBy(s => s.Unit.Code, StringComparer.OrdinalIgnoreCase))
        {
            var record = await store.Get(scope.Unit.Id, http.RequestAborted);
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
                ["editable"] = scope.Edit && !context.ReadOnly && record?.SubmittedAt is null,
                ["enviado_en"] = record?.SubmittedAt,
                ["porcentaje"] = record?.Progress ?? 0,
                ["actualizado_en"] = record?.UpdatedAt,
                ["actualizado_por"] = record?.UpdatedBy,
                ["url"] = "/formatos/" + Uri.EscapeDataString(scope.Unit.Id)
            });
        }
        var admin = context.Profile.Has("administrador");
        return new PageData("Inicio", new()
        {
            ["formatos"] = rows,
            ["consultaInstitucional"] = context.Profile.InstitutionalRead,
            ["administrador"] = admin,
            ["directorio"] = context.Profile.InstitutionalRead ? rows : [],
            ["urAdministracion"] = admin ? context.Directory.AdministratorRoot(context.Profile.User)?.Public() : null,
            ["puedeColaboradores"] = admin || context.Profile.Responsible && (access.Selection is { Kind: "preview" } preview
                ? context.Directory.Get(preview.UnitId) is { } selected && UnitDirectory.IsForm(selected)
                : context.Directory.Responsibilities(context.Profile.User).Any()),
            ["sincronizadoEn"] = await catalogs.LastSii(http.RequestAborted)
        }, context.Profile);
    }

    public async Task<PageData> Show(string ur)
    {
        var context = await access.Resolve(http);
        if (!context.Scopes.TryGetValue(ur, out var scope)) throw new DomainProblem(403, "No tienes acceso al formato de esta UR.");
        var record = await store.Get(ur, http.RequestAborted);
        var content = record?.Content.DeepClone() ?? schema.Blank(scope.Unit);
        if (record?.SubmittedAt is null) content["encabezado"]!["area"] = scope.Unit.Description ?? scope.Unit.Code;
        object inventory = record?.SubmittedAt is null
            ? LocalCatalog.Merge(content.AsObject(), await catalogs.Inventory(scope.Unit, http.RequestAborted)).Status
            : new { estado = "enviado", nuevos = 0, total = content["identificacion"]!.AsArray().Count, aviso = (string?)null };
        var snapshot = record?.SubmissionSnapshot is { } frozen ? JsonNode.Parse(frozen) : null;
        return new PageData("FormatoUR", new()
        {
            ["unidad"] = (object?)snapshot?["unidad"] ?? scope.Unit.Public(),
            ["contenido"] = content,
            ["plantilla"] = schema.Blank(scope.Unit),
            ["definicion"] = schema.Definition(),
            ["editable"] = scope.Edit && !context.ReadOnly && record?.SubmittedAt is null,
            ["puedeEnviar"] = scope.Edit && !context.ReadOnly && record?.SubmittedAt is null && new FormAccess(context.Directory).CanSubmit(context.Profile, scope.Unit),
            ["enviadoEn"] = record?.SubmittedAt,
            ["permisoEdicion"] = scope.Edit,
            ["version"] = record?.Version ?? 0,
            ["porcentaje"] = record?.Progress ?? 0,
            ["actualizadoEn"] = record?.UpdatedAt,
            ["actualizadoPor"] = record?.UpdatedBy,
            ["guardarUrl"] = "/formatos/" + Uri.EscapeDataString(ur),
            ["ilda"] = inventory
        }, context.Profile);
    }

}
