using Tdv2.Integrations.Nexo;
using System.Text.Json.Nodes;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
using Tdv2.Synchronization;
using Tdv2.Web;
namespace Tdv2.Services;

/// <summary>Coordina las concesiones locales y centrales de colaboradores dentro de una solicitud autorizada.</summary>
public sealed class CollaboratorService(RequestAccess access, CollaboratorAccess delegation, NexoOperations nexo, DatabaseConnections connections, AccessState state, OperationAudit audit, PostgresCollaborationStore store, IHttpContextAccessor accessor)
{
    // RequestAccess es scoped: la identidad y Nexo se revalidan en cada solicitud, nunca en un singleton.
    private HttpContext http => accessor.HttpContext ?? throw new InvalidOperationException("No hay una solicitud activa.");

    public async Task<PageData> Index()
    {
        var context = await access.Resolve(http); var roots = await delegation.Roots(http); var profile = context.Profile;
        var admin = profile.Has("administrador"); var branch = admin ? context.Directory.AdministratorRoot(profile.User) : null;
        var kinds = access.Selection?.Kind == "preview" ? new[] { "local", "dependencias" } : roots.Count > 0 ? (await delegation.Roles(http.RequestAborted)).Keys.ToArray() : [];
        kinds = kinds.Where(kind => kind == "local" || roots.Values.Any(u => u.Level == 2)).ToArray();
        string? notice = null;
        if (roots.Count == 0)
        {
            notice = admin && branch is null ? "No se pudo identificar tu rama de nivel 2. Revisa en Nexo tu número de empleado y un área de pertenencia única."
                : admin && UnitDirectory.Employee(profile.User) != branch?.Employee ? "Tu cuenta es Administrador, pero no figura como encargada de esta área de nivel 2. El rol Administrador por sí solo no concede administración delegada."
                : "Nexo no publicó una delegación válida para tu cuenta. Revisa los roles autorizados y la responsabilidad institucional.";
        }
        else if (kinds.Length == 0) notice = "No hay roles de colaborador habilitados para asignar en la administración delegada de Nexo.";
        var links = roots.Count > 0 ? await store.List(roots.Keys.ToArray(), context.Directory, http.RequestAborted) : [];
        return new PageData("Colaboradores", new()
        {
            ["unidades"] = roots.Values.Select(u => u.Public()).ToArray(),
            ["tipos"] = kinds,
            ["administrador"] = admin,
            ["urAdministracion"] = branch?.Public(),
            ["avisoDelegacion"] = notice,
            ["colaboraciones"] = links
        }, profile);
    }

    public async Task<object> Search(string unit, string query, int page)
    {
        var context = await access.Resolve(http);
        if (access.Selection?.Kind == "preview") throw new DomainProblem(403, "La búsqueda y la asignación de personas están desactivadas durante la vista de prueba.");
        await delegation.Root(http, unit);
        var rows = await delegation.Call(http, "buscar_personas", unit, new() { ["q"] = query, ["pagina"] = page }, [access.Actor(http), unit, query, page]);
        var people = new List<object>();
        foreach (var row in rows.OfType<JsonObject>())
        {
            var origin = NexoOperations.Text(row, "id_ur"); var owner = context.Directory.LocalCollaborationUnit(origin, unit);
            if (origin is null || owner is null || !context.Directory.Within(origin, unit) || !context.Directory.Within(owner.Id, unit)) continue;
            people.Add(new
            {
                email = NexoOperations.Text(row, "email"),
                nombre = NexoOperations.Text(row, "nombre"),
                id_ur = origin,
                adscripcion = NexoOperations.Text(row, "desc_ur"),
                formato = owner.Description ?? owner.Code
            });
        }
        await audit.Record("buscar", "colaboracion", unit, "confirmado", http.RequestAborted, new { cantidad = people.Count });
        return new { personas = people, hayMas = rows.FirstOrDefault() is JsonObject first && NexoOperations.Number(first, "total") > page * 25 };
    }

    public async Task<object> Add(JsonObject input)
    {
        var context = await access.Resolve(http);
        var unit = Inputs.Text(input, "ur", 1, 32); var email = PostgresNexoProfiles.Email(Inputs.Text(input, "email", 3, 254));
        var origin = Inputs.Text(input, "id_ur", 1, 32); var kind = Inputs.Text(input, "tipo", 1, 24);
        if (kind is not ("local" or "dependencias")) throw Inputs.Invalid("tipo", "Selecciona un tipo de colaboración válido.");
        var root = await delegation.Root(http, unit); var roles = await delegation.Roles(http.RequestAborted);
        if (!roles.TryGetValue(kind, out var role) || kind == "dependencias" && root.Level != 2) throw new DomainProblem(403, "El rol no está habilitado para asignar en esta área.");
        var owner = context.Directory.LocalCollaborationUnit(origin, unit);
        if (owner is null || !context.Directory.Within(origin, unit) || !context.Directory.Within(owner.Id, unit)) throw new DomainProblem(403, "El área seleccionada no pertenece a esta rama.");
        var scope = kind == "local" ? owner.Id : unit;
        await using var connection = await connections.Open("Tdv2", http.RequestAborted);
        await store.Lock(connection, email, origin, role, false, http.RequestAborted);
        try
        {
            await using var transaction = await connection.BeginTransactionAsync(http.RequestAborted);
            await state.Guard(connection, transaction, http.RequestAborted);
            var result = await delegation.Call(http, "conceder_acceso", unit, new() { ["email"] = email, ["rol_id"] = role, ["ur_origen"] = origin }, [access.Actor(http), unit, email, role, origin]);
            var grant = result.FirstOrDefault() is JsonObject row ? NexoOperations.Number(row, "concesion_id") : 0;
            if (grant <= 0) throw new DomainProblem(503, "Nexo no confirmó la concesión.");
            var people = await nexo.Rows("usuarios", email, http.RequestAborted);
            if (people.Count != 1 || people[0] is not JsonObject person || NexoOperations.Text(person, "email") != email)
                throw new DomainProblem(503, "Nexo no confirmó la identidad del colaborador. Reintenta el alta.");
            var user = new Person(email, NexoOperations.Text(person, "nombre") ?? email, NexoOperations.Text(person, "num_empleado"),
                NexoOperations.Text(person, "ID_UR"), NexoOperations.Text(person, "origen_ur") ?? "", NexoOperations.Text(person, "tipo_cuenta") ?? "");
            if (UnitDirectory.Employee(user) is not { } employee || context.Directory.Affiliation(user)?.Id != origin)
                throw Inputs.Invalid("colaborador", "Nexo debe publicar el número de empleado y un área de pertenencia que coincida con la seleccionada. Todavía no se habilitó el llenado en TDV2.");
            var id = await store.Save(connection, transaction, user, employee, origin, scope, kind, grant, role, access.Actor(http), unit, http.RequestAborted);
            await audit.Write(connection, transaction, "crear", "colaboracion", id.ToString(), "confirmado", http.RequestAborted, details: new { nexo_concesion_id = grant });
            await transaction.CommitAsync(http.RequestAborted);
            return new { message = "Colaborador agregado.", id_ur_formato = scope };
        }
        finally { await store.Lock(connection, email, origin, role, true, CancellationToken.None); }
    }

    public async Task<bool> Remove(long collaboration)
    {
        await access.Resolve(http);
        await using var connection = await connections.Open("Tdv2", http.RequestAborted);
        var link = await store.Find(connection, collaboration, http.RequestAborted) ?? throw new DomainProblem(404, "No se encontró la colaboración.");
        var unit = NexoOperations.Text(link, "ur_otorgante")!; await delegation.Root(http, unit);
        var email = NexoOperations.Text(link, "email")!; var origin = NexoOperations.Text(link, "id_ur_origen")!; var role = NexoOperations.Number(link, "nexo_rol_id");
        await store.Lock(connection, email, origin, role, false, http.RequestAborted);
        try
        {
            link = await store.Find(connection, collaboration, http.RequestAborted) ?? throw new DomainProblem(404, "No se encontró la colaboración.");
            if (NexoOperations.Text(link, "ur_otorgante") != unit || NexoOperations.Text(link, "email") != email) throw new DomainProblem(409, "La colaboración cambió. Recarga la lista.");
            var grant = NexoOperations.Number(link, "nexo_concesion_id");
            // Confirmar primero la revocación local: una falla central jamás conserva acceso al formato.
            await using (var transaction = await connection.BeginTransactionAsync(http.RequestAborted))
            {
                await state.Guard(connection, transaction, http.RequestAborted);
                await store.Revoke(connection, transaction, collaboration, http.RequestAborted);
                await audit.Write(connection, transaction, "revocar_local", "colaboracion", collaboration.ToString(), "retiro_central_pendiente", http.RequestAborted);
                await transaction.CommitAsync(http.RequestAborted);
            }
            try
            {
                await using var transaction = await connection.BeginTransactionAsync(http.RequestAborted);
                await state.Guard(connection, transaction, http.RequestAborted);
                var other = await store.HasOtherActiveLink(connection, transaction, grant, http.RequestAborted);
                if (!other) await delegation.Call(http, "retirar_acceso", unit, new() { ["concesion_id"] = grant }, [access.Actor(http), unit, grant]);
                await store.ConfirmRemoval(connection, transaction, collaboration, http.RequestAborted);
                await audit.Write(connection, transaction, "retirar", "colaboracion", collaboration.ToString(), "confirmado", http.RequestAborted, details: new { concesion_conservada = other });
                await transaction.CommitAsync(http.RequestAborted);
                return false;
            }
            catch (Exception e) when (e is DomainProblem or NpgsqlException)
            {
                return true;
            }
        }
        finally { await store.Lock(connection, email, origin, role, true, CancellationToken.None); }
    }
}
