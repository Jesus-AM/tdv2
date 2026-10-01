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
                ["ejercicio"] = scope.Unit.Year,
                ["editable"] = scope.Edit && !context.ReadOnly,
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
            ["consultaInstitucional"] = admin || context.Profile.Has("consulta_institucional"),
            ["administrador"] = admin,
            ["directorio"] = admin ? context.Directory.Units.Values.Where(UnitDirectory.IsForm).Select(u => u.Public()).ToArray() : [],
            ["urAdministracion"] = admin ? context.Directory.AdministratorRoot(context.Profile.User)?.Public() : null,
            ["puedeColaboradores"] = admin || context.Profile.Has("responsable_ur") && context.Directory.Responsibilities(context.Profile.User).Any(u => u.Level == 2),
            ["sincronizadoEn"] = await catalogs.LastSii(http.RequestAborted)
        }, context.Profile);
    }

    public async Task<PageData> Show(string ur)
    {
        var context = await access.Resolve(http);
        if (!context.Scopes.TryGetValue(ur, out var scope)) throw new DomainProblem(403, "No tienes acceso al formato de esta UR.");
        var record = await store.Get(ur, http.RequestAborted);
        var content = record?.Content.DeepClone() ?? schema.Blank(scope.Unit);
        content["encabezado"]!["area"] = scope.Unit.Description ?? scope.Unit.Code;
        var merged = LocalCatalog.Merge(content.AsObject(), await catalogs.Inventory(scope.Unit, http.RequestAborted));
        return new PageData("FormatoUR", new()
        {
            ["unidad"] = scope.Unit.Public(),
            ["contenido"] = content,
            ["plantilla"] = schema.Blank(scope.Unit),
            ["definicion"] = schema.Definition(),
            ["editable"] = scope.Edit && !context.ReadOnly,
            ["permisoEdicion"] = scope.Edit,
            ["version"] = record?.Version ?? 0,
            ["porcentaje"] = record?.Progress ?? 0,
            ["actualizadoEn"] = record?.UpdatedAt,
            ["actualizadoPor"] = record?.UpdatedBy,
            ["guardarUrl"] = "/formatos/" + Uri.EscapeDataString(ur),
            ["ilda"] = merged.Status
        }, context.Profile);
    }

    public async Task<object> Save(string ur, SaveForm input)
    {
        var context = await access.Resolve(http);
        if (!context.Scopes.TryGetValue(ur, out var scope) || !scope.Edit) throw new DomainProblem(403, "No tienes permiso para editar este formato.");
        if (input.Version is null or < 0 || input.Contenido is null) throw new DomainProblem(422, "Revisa la versión y el contenido del formato.");
        var valid = schema.Validate(input.Contenido, scope.Unit);
        var saved = await store.Save(ur, input.Version.Value, valid, access.Actor(http), http.RequestAborted);
        return new { version = saved.Version, porcentaje = saved.Progress, actualizadoEn = saved.UpdatedAt, actualizadoPor = saved.UpdatedBy };
    }
}
