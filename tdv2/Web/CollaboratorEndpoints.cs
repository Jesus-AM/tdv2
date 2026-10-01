using System.Text.Json.Nodes;
using Npgsql;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
namespace Tdv2.Web;

public sealed class CollaboratorAccess(RequestAccess access, NexoOperations nexo)
{
    public async Task<Dictionary<string, Unit>> Roots(HttpContext http)
    {
        var context = await access.Resolve(http); var profile = context.Profile; var directory = context.Directory;
        var candidates = new Dictionary<string, Unit>(StringComparer.Ordinal);
        if (profile.Has("administrador"))
        {
            if (directory.AdministratorRoot(profile.User) is { } root) candidates.Add(root.Id, root);
        }
        else if (profile.Has("responsable_ur"))
        {
            var units = access.Selection is { Kind: "preview" } selection ? new[] { directory.Get(selection.UnitId)! } : directory.Responsibilities(profile.User);
            foreach (var unit in units.Where(u => u.Level == 2)) candidates.Add(unit.Id, unit);
        }
        if (candidates.Count == 0 && !profile.Has("administrador")) throw new DomainProblem(403, "No tienes autorización para administrar colaboradores.");
        if (access.Selection?.Kind == "preview" || candidates.Count == 0) return candidates;
        var rows = await nexo.Rows("delegacion", profile.User.Email, http.RequestAborted);
        var employee = UnitDirectory.Employee(profile.User);
        return candidates.Where(pair => rows.OfType<JsonObject>().Count(row => NexoOperations.Text(row, "id_ur") == pair.Key
            && employee is not null && NexoOperations.Text(row, "num_empleado") == employee && employee == pair.Value.Employee
            && NexoOperations.Number(row, "nivel_ur") == pair.Value.Level) == 1).ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
    }
    public async Task<Unit> Root(HttpContext http, string unit) => (await Roots(http)).GetValueOrDefault(unit)
        ?? throw new DomainProblem(403, "El área no está habilitada para delegar en Nexo y TDV2.");
    public async Task<Dictionary<string, long>> Roles(CancellationToken ct)
    {
        var rows = await nexo.Rows("delegacion_roles", null, ct); var result = new Dictionary<string, long>();
        foreach (var (kind, key) in new[] { ("local", "colaborador_local"), ("dependencias", "colaborador_dependencias") })
        {
            var matches = rows.OfType<JsonObject>().Where(r => NexoOperations.Text(r, "clave") == key).ToArray();
            if (matches.Length > 1 || matches.Length == 1 && NexoOperations.Number(matches[0], "rol_id") <= 0) throw new DomainProblem(503, "Nexo no publicó roles de delegación válidos.");
            if (matches.Length == 1) result[kind] = NexoOperations.Number(matches[0], "rol_id");
        }
        return result;
    }
    public async Task<JsonArray> Call(HttpContext http, string operation, string unit, JsonObject data, object[] ownParameters)
    {
        if (access.Selection is not { Kind: "representation" } selection) return await nexo.Delegate(operation, ownParameters, http.RequestAborted);
        data["ur"] = unit; data["token"] = selection.Token;
        return await nexo.Represent(operation, access.Actor(http), data, http.RequestAborted) as JsonArray
            ?? throw new DomainProblem(503, "Nexo no confirmó el resultado de la operación delegada.");
    }
}

public static class CollaboratorEndpoints
{
    private static async Task<JsonObject?> Link(NpgsqlConnection connection, long id, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT to_jsonb(c)::text FROM colaboraciones_ur c WHERE id=$1", connection);
        command.Parameters.AddWithValue(id); return await command.ExecuteScalarAsync(ct) is string text ? JsonNode.Parse(text)!.AsObject() : null;
    }
    private static async Task Lock(NpgsqlConnection connection, string email, string origin, long role, bool release, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("SELECT pg_advisory_" + (release ? "unlock" : "lock") + "(hashtextextended($1,1))", connection);
        command.Parameters.AddWithValue(email + "|" + origin + "|" + role); await command.ExecuteNonQueryAsync(ct);
    }
    public static void MapCollaborators(this WebApplication app)
    {
        app.MapGet("/colaboradores", async (HttpContext http, RequestAccess access, CollaboratorAccess delegation, DatabaseConnections connections) =>
        {
            var context = await access.Resolve(http); var roots = await delegation.Roots(http); var profile = context.Profile;
            var admin = profile.Has("administrador"); var branch = admin ? context.Directory.AdministratorRoot(profile.User) : null;
            var kinds = access.Selection?.Kind == "preview" ? new[] { "local", "dependencias" } : roots.Count > 0 ? (await delegation.Roles(http.RequestAborted)).Keys.ToArray() : [];
            string? notice = null;
            if (roots.Count == 0)
            {
                notice = admin && branch is null ? "No se pudo identificar tu rama de nivel 2. Revisa en Nexo tu número de empleado y un área de pertenencia única."
                    : admin && UnitDirectory.Employee(profile.User) != branch?.Employee ? "Tu cuenta es Administrador, pero no figura como encargada de esta área de nivel 2. El rol Administrador por sí solo no concede administración delegada."
                    : "Nexo no publicó una delegación válida para tu cuenta. Revisa los roles autorizados y la responsabilidad institucional.";
            }
            else if (kinds.Length == 0) notice = "No hay roles de colaborador habilitados para asignar en la administración delegada de Nexo.";
            var links = new List<object>();
            if (roots.Count > 0)
            {
                await using var connection = await connections.Open("Tdv2", http.RequestAborted);
                await using var command = new NpgsqlCommand("SELECT id,email,nombre,tipo,ur_otorgante,id_ur_alcance,revocada_en,retiro_central_pendiente FROM colaboraciones_ur WHERE ur_otorgante=ANY($1) AND (revocada_en IS NULL OR retiro_central_pendiente=true) ORDER BY nombre,id", connection);
                command.Parameters.AddWithValue(roots.Keys.ToArray()); await using var reader = await command.ExecuteReaderAsync(http.RequestAborted);
                while (await reader.ReadAsync(http.RequestAborted)) links.Add(new { id = reader.GetInt64(0), email = reader.GetString(1), nombre = reader.GetString(2), tipo = reader.GetString(3),
                    ur_otorgante = reader.GetString(4), alcance = context.Directory.Get(reader.GetString(5))?.Description ?? reader.GetString(5), revocada = !reader.IsDBNull(6), pendiente = reader.GetBoolean(7) });
            }
            return PageResponse.Page(http, "Colaboradores", new() { ["unidades"] = roots.Values.Select(u => u.Public()).ToArray(), ["tipos"] = kinds,
                ["administrador"] = admin, ["urAdministracion"] = branch?.Public(), ["avisoDelegacion"] = notice, ["colaboraciones"] = links }, profile);
        });
        app.MapGet("/colaboradores/personas", async (HttpContext http, RequestAccess access, CollaboratorAccess delegation, OperationAudit audit) =>
        {
            var context = await access.Resolve(http);
            if (access.Selection?.Kind == "preview") throw new DomainProblem(403, "La búsqueda y la asignación de personas están desactivadas durante la vista de prueba.");
            var unit = Inputs.Query(http, "ur", 1, 32); var query = Inputs.Query(http, "q", 2, 150); var page = Inputs.Page(http, "page");
            await delegation.Root(http, unit);
            var rows = await delegation.Call(http, "buscar_personas", unit, new() { ["q"] = query, ["pagina"] = page }, [access.Actor(http), unit, query, page]);
            var people = new List<object>();
            foreach (var row in rows.OfType<JsonObject>())
            {
                var origin = NexoOperations.Text(row, "id_ur"); var owner = context.Directory.FormUnit(origin);
                if (origin is null || owner is null || !context.Directory.Within(origin, unit) || !context.Directory.Within(owner.Id, unit)) continue;
                people.Add(new { email = NexoOperations.Text(row, "email"), nombre = NexoOperations.Text(row, "nombre"), id_ur = origin,
                    adscripcion = NexoOperations.Text(row, "desc_ur"), formato = owner.Description ?? owner.Code });
            }
            await audit.Record("buscar", "colaboracion", unit, "confirmado", http.RequestAborted, new { cantidad = people.Count });
            return Results.Json(new { personas = people, hayMas = rows.FirstOrDefault() is JsonObject first && NexoOperations.Number(first, "total") > page * 25 });
        }).RequireRateLimiting("collaborator-search");
        app.MapPost("/colaboradores", async (JsonObject input, HttpContext http, RequestAccess access, CollaboratorAccess delegation,
            NexoOperations nexo, DatabaseConnections connections, AccessState state, OperationAudit audit) =>
        {
            var context = await access.Resolve(http);
            var unit = Inputs.Text(input, "ur", 1, 32); var email = PostgresNexoProfiles.Email(Inputs.Text(input, "email", 3, 254));
            var origin = Inputs.Text(input, "id_ur", 1, 32); var kind = Inputs.Text(input, "tipo", 1, 24);
            if (kind is not ("local" or "dependencias")) throw Inputs.Invalid("tipo", "Selecciona un tipo de colaboración válido.");
            var root = await delegation.Root(http, unit); var roles = await delegation.Roles(http.RequestAborted);
            if (!roles.TryGetValue(kind, out var role) || kind == "dependencias" && root.Level != 2) throw new DomainProblem(403, "El rol no está habilitado para asignar en esta área.");
            var owner = context.Directory.FormUnit(origin);
            if (owner is null || !context.Directory.Within(origin, unit) || !context.Directory.Within(owner.Id, unit)) throw new DomainProblem(403, "El área seleccionada no pertenece a esta rama.");
            var scope = kind == "local" ? owner.Id : unit;
            await using var connection = await connections.Open("Tdv2", http.RequestAborted);
            await Lock(connection, email, origin, role, false, http.RequestAborted);
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
                await using var command = new NpgsqlCommand("""
                    INSERT INTO colaboraciones_ur(email,num_empleado,nombre,id_ur_origen,id_ur_alcance,tipo,nexo_concesion_id,nexo_rol_id,otorgado_por,ur_otorgante,created_at,updated_at)
                    VALUES($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,timezone('UTC',now()),timezone('UTC',now()))
                    ON CONFLICT(nexo_concesion_id,id_ur_alcance) DO UPDATE SET email=EXCLUDED.email,num_empleado=EXCLUDED.num_empleado,nombre=EXCLUDED.nombre,
                      id_ur_origen=EXCLUDED.id_ur_origen,tipo=EXCLUDED.tipo,nexo_rol_id=EXCLUDED.nexo_rol_id,otorgado_por=EXCLUDED.otorgado_por,
                      ur_otorgante=EXCLUDED.ur_otorgante,revocada_en=null,retiro_central_pendiente=false,updated_at=EXCLUDED.updated_at RETURNING id
                    """, connection, transaction);
                foreach (var parameter in new object[] { email, employee, user.Name, origin, scope, kind, grant, role, access.Actor(http), unit }) command.Parameters.AddWithValue(parameter);
                var id = Convert.ToInt64(await command.ExecuteScalarAsync(http.RequestAborted));
                await audit.Write(connection, transaction, "crear", "colaboracion", id.ToString(), "confirmado", http.RequestAborted, details: new { nexo_concesion_id = grant });
                await transaction.CommitAsync(http.RequestAborted);
                return Results.Json(new { message = "Colaborador agregado.", id_ur_formato = scope }, statusCode: 201);
            }
            finally { await Lock(connection, email, origin, role, true, CancellationToken.None); }
        }).RequireRateLimiting("collaborator-writes");
        app.MapDelete("/colaboradores/{collaboration:long}", async (long collaboration, HttpContext http, RequestAccess access, CollaboratorAccess delegation,
            DatabaseConnections connections, AccessState state, OperationAudit audit) =>
        {
            await access.Resolve(http);
            await using var connection = await connections.Open("Tdv2", http.RequestAborted);
            var link = await Link(connection, collaboration, http.RequestAborted) ?? throw new DomainProblem(404, "No se encontró la colaboración.");
            var unit = NexoOperations.Text(link, "ur_otorgante")!; await delegation.Root(http, unit);
            var email = NexoOperations.Text(link, "email")!; var origin = NexoOperations.Text(link, "id_ur_origen")!; var role = NexoOperations.Number(link, "nexo_rol_id");
            await Lock(connection, email, origin, role, false, http.RequestAborted);
            try
            {
                link = await Link(connection, collaboration, http.RequestAborted) ?? throw new DomainProblem(404, "No se encontró la colaboración.");
                if (NexoOperations.Text(link, "ur_otorgante") != unit || NexoOperations.Text(link, "email") != email) throw new DomainProblem(409, "La colaboración cambió. Recarga la lista.");
                var grant = NexoOperations.Number(link, "nexo_concesion_id");
                await using (var transaction = await connection.BeginTransactionAsync(http.RequestAborted))
                {
                    await state.Guard(connection, transaction, http.RequestAborted);
                    await using var command = new NpgsqlCommand("UPDATE colaboraciones_ur SET revocada_en=timezone('UTC',now()),updated_at=timezone('UTC',now()),retiro_central_pendiente=true WHERE id=$1", connection, transaction);
                    command.Parameters.AddWithValue(collaboration); await command.ExecuteNonQueryAsync(http.RequestAborted);
                    await audit.Write(connection, transaction, "revocar_local", "colaboracion", collaboration.ToString(), "retiro_central_pendiente", http.RequestAborted);
                    await transaction.CommitAsync(http.RequestAborted);
                }
                try
                {
                    await using var transaction = await connection.BeginTransactionAsync(http.RequestAborted);
                    await state.Guard(connection, transaction, http.RequestAborted);
                    await using var check = new NpgsqlCommand("SELECT 1 FROM colaboraciones_ur WHERE nexo_concesion_id=$1 AND revocada_en IS NULL LIMIT 1", connection, transaction);
                    check.Parameters.AddWithValue(grant); var other = await check.ExecuteScalarAsync(http.RequestAborted) is not null;
                    if (!other) await delegation.Call(http, "retirar_acceso", unit, new() { ["concesion_id"] = grant }, [access.Actor(http), unit, grant]);
                    await using var clear = new NpgsqlCommand("UPDATE colaboraciones_ur SET retiro_central_pendiente=false,updated_at=timezone('UTC',now()) WHERE id=$1", connection, transaction);
                    clear.Parameters.AddWithValue(collaboration); await clear.ExecuteNonQueryAsync(http.RequestAborted);
                    await audit.Write(connection, transaction, "retirar", "colaboracion", collaboration.ToString(), "confirmado", http.RequestAborted, details: new { concesion_conservada = other });
                    await transaction.CommitAsync(http.RequestAborted);
                    return Results.Json(new { message = "Colaboración retirada." });
                }
                catch (Exception e) when (e is DomainProblem or NpgsqlException)
                {
                    return Results.Json(new { message = "El acceso a este formato ya está retirado. Quedó pendiente el retiro en Nexo; puedes reintentarlo desde esta lista." }, statusCode: 202);
                }
            }
            finally { await Lock(connection, email, origin, role, true, CancellationToken.None); }
        }).RequireRateLimiting("collaborator-writes");
    }
}
