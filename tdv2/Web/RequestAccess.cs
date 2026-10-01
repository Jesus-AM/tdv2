using System.Security.Claims;
using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
namespace Tdv2.Web;

public sealed record AccessContext(Profile Profile, UnitDirectory Directory, IReadOnlyDictionary<string, Scope> Scopes, bool ReadOnly = false);
/// <summary>Resuelve cuenta real y perfil efectivo por solicitud; una caída de Nexo siempre deniega.</summary>
/// <remarks>Su caché sólo dura el scope HTTP. No trasladarla a sesión ni aceptar roles del navegador.</remarks>
public sealed class RequestAccess(INexoProfiles nexo, IFormStore store, ISessionIdentity identity, AccessState state, NexoOperations operations, OperationAudit audit)
{
    // La cuenta Microsoft sigue siendo el actor auditado aunque Nexo autorice representar a otra persona.
    public Profile? Real { get; private set; }
    public Profile? Effective { get; private set; }
    public AccessSelection? Selection { get; private set; }
    public JsonObject? Capability { get; private set; }
    public UnitDirectory? Directory { get; private set; }
    private AccessContext? scopes;
    public string Actor(HttpContext http) => http.User.FindFirstValue(ClaimTypes.Email) ?? "";
    public bool ReadOnly => Selection is { Kind: "preview" } || Selection is { Kind: "representation", Write: false };
    public async Task<Profile> Own(HttpContext http)
    {
        if (Real is not null) return Real;
        if (http.User.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(Actor(http))
            || !await identity.Matches(http.User, http.RequestAborted)) throw new DomainProblem(401, "Inicia sesión con tu cuenta institucional de Microsoft 365.");
        Real = await nexo.ForEmail(Actor(http), http.RequestAborted);
        if (Real.User.Email != Actor(http) || Real.User.AccountType != "individual" || Real.Roles.Count == 0)
            throw new DomainProblem(403, "No tienes un acceso vigente en Nexo.");
        return Real;
    }
    public async Task<Profile> Profile(HttpContext http, string? module = null)
    {
        if (Effective is null)
        {
            var snapshot = await state.Load(http.RequestAborted);
            var safe = HttpMethods.IsGet(http.Request.Method) || HttpMethods.IsHead(http.Request.Method) || HttpMethods.IsOptions(http.Request.Method);
            if (!safe && http.Request.Headers["X-TDV2-Context"].ToString() is var sent
                && (sent.Length == 0 ? "own" : sent) != snapshot.Key)
                throw new DomainProblem(409, "El usuario activo cambió en otra pestaña. Recarga antes de guardar.");
            var real = await Own(http); Selection = snapshot.Selection;
            audit.Resolved = Selection; audit.Effective = real;
            var profile = real;
            if (Selection is { } selection)
            {
                if (selection.ActorEmail != Actor(http)) throw new DomainProblem(409, "El contexto cambió. Vuelve a tu usuario.");
                if (!ModuleAccess.Allows(real, "pruebas_acceso")) throw new DomainProblem(403, "Tu autorización para Pruebas de acceso cambió. Vuelve a tu usuario.");
                if (selection.Kind == "preview")
                {
                    if (!safe) throw new DomainProblem(403, "La vista de prueba es de solo lectura. Sal de ella para guardar cambios.");
                    if (selection.ExpiresAt <= DateTimeOffset.UtcNow) throw new DomainProblem(409, "La vista de prueba venció. Vuelve a tu usuario.");
                    if (!ModuleAccess.Allows(real, "procesos_operativos")) throw new DomainProblem(403, "No tienes acceso a Procesos operativos.");
                    try { profile = ScenarioRoles.Resolve(selection, await Units(http), real); }
                    catch (DomainProblem) { throw new DomainProblem(409, "El rol o el área de la vista ya no están disponibles. Vuelve a tu usuario."); }
                }
                else if (selection.Kind == "representation")
                {
                    if (selection.ExpiresAt <= DateTimeOffset.UtcNow || string.IsNullOrEmpty(selection.Token))
                        throw new DomainProblem(409, "La representación venció. Vuelve a tu usuario.");
                    try
                    {
                        var response = (await operations.Represent("validar", Actor(http), new() { ["token"] = selection.Token, ["requiere_escritura"] = !safe }, http.RequestAborted)).AsObject();
                        Selection = ParseRepresentation(response, Actor(http), selection.Email);
                        if (Selection.Id != selection.Id || Selection.ExpiresAt > selection.ExpiresAt.AddSeconds(1))
                            throw new DomainProblem(409, "La representación cambió. Vuelve a tu usuario.");
                        Selection = Selection with { Token = selection.Token, Write = Selection.Write && selection.Write };
                        if (!safe && !Selection.Write) throw new DomainProblem(409, "La representación es de solo lectura. Vuelve a tu usuario.");
                        profile = await nexo.ForEmail(selection.Email, http.RequestAborted);
                    }
                    catch (DomainProblem p) when (p.Status is 403 or 422)
                    { throw new DomainProblem(409, "La representación venció o ya no está autorizada. Vuelve a tu usuario."); }
                    catch (DomainProblem p) when (p.Status == 503)
                    { throw new DomainProblem(503, "No se pudo comprobar la representación. Las operaciones están bloqueadas; puedes volver a tu usuario."); }
                }
                else throw new DomainProblem(409, "El contexto no es válido. Vuelve a tu usuario.");
            }
            else if (ModuleAccess.Allows(real, "pruebas_acceso"))
                Capability = await operations.Capability(Actor(http), http.RequestAborted);
            Effective = profile; audit.Effective = profile; audit.Resolved = Selection;
        }
        if (module is not null && !ModuleAccess.Allows(Selection?.Kind == "preview" ? Real! : Effective, module))
            throw new DomainProblem(403, "No tienes acceso a este módulo de Transformación Digital.");
        return Effective;
    }
    public async Task<UnitDirectory> Units(HttpContext http) => Directory ??= new(await store.Units(http.RequestAborted));
    public async Task<AccessContext> Resolve(HttpContext http)
    {
        var profile = await Profile(http, "procesos_operativos");
        if (scopes is not null) return scopes;
        var directory = await Units(http);
        var rules = new FormAccess(directory);
        if (Selection is { Kind: "preview" } scenario)
            return scopes = new(profile, directory, rules.Scenario(profile, directory.Get(scenario.UnitId)!), true);
        var links = profile.Has("administrador") ? [] : await store.Collaborations(profile.User.Email, http.RequestAborted);
        var grants = links.Count == 0 ? [] : await nexo.Grants(profile.User.Email, http.RequestAborted);
        return scopes = new(profile, directory, rules.Scopes(profile, links, grants), ReadOnly);
    }
    public static AccessSelection ParseRepresentation(JsonObject data, string actor, string email, bool requireToken = false)
    {
        var id = NexoOperations.Text(data, "id"); var token = NexoOperations.Text(data, "token");
        if (!Guid.TryParse(id, out _) || NexoOperations.Text(data, "actor_email") != actor || NexoOperations.Text(data, "email") != email
            || data["escritura"] is not JsonValue write || !write.TryGetValue<bool>(out _)
            || !DateTimeOffset.TryParse(NexoOperations.Text(data, "expira_en"), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var expiry)
            || expiry <= DateTimeOffset.UtcNow || requireToken && (string.IsNullOrWhiteSpace(token) || token.Length > 8192))
            throw new DomainProblem(503, "Nexo no confirmó una representación válida.");
        return new("representation", id!, actor, email, NexoOperations.Text(data, "nombre") ?? email,
            NexoOperations.Text(data, "id_ur"), NexoOperations.Flag(data, "escritura"), expiry, token, Reason: NexoOperations.Text(data, "motivo"));
    }
    public async Task EnsureOwn(HttpContext http)
    {
        var snapshot = await state.Load(http.RequestAborted);
        if (snapshot.Selection is not null) throw new DomainProblem(409, "Vuelve a tu usuario antes de administrar la configuración o iniciar otra prueba.");
    }
}
