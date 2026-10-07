namespace Tdv2.Domain;

// Estado cifrado en servidor. Nunca serializar este registro completo en páginas ni auditorías.
public sealed record AccessSelection(string Kind, string Id, string ActorEmail, string Email, string Name,
    string? UnitId, bool Write, DateTimeOffset ExpiresAt, string? Token = null, string? Role = null, string? Reason = null)
{
    public object? Representation() => Kind == "representation" ? new
    {
        id = Id,
        email = Email,
        nombre = Name,
        id_ur = UnitId,
        escritura = Write,
        expira_en = ExpiresAt
    } : null;
    public object? Preview(UnitDirectory? directory = null) => Kind == "preview" ? new
    {
        mode = "escenario",
        label = ScenarioRoles.Label(Role!),
        roleLabel = ScenarioRoles.Label(Role!),
        unit = directory?.Get(UnitId)?.Public(),
        expiresAt = ExpiresAt.ToUnixTimeSeconds(),
        readOnly = true
    } : null;
}
public sealed record AccessSnapshot(long Revision, AccessSelection? Selection)
{
    public string Key => Selection is { Kind: "representation" } s ? s.Id : "own";
}
public static class ScenarioRoles
{
    public static readonly IReadOnlyDictionary<string, (string Key, string Label)> All = new Dictionary<string, (string, string)>
    {
        ["responsable"] = ("responsable_ur", "Responsable de UR"),
        ["responsable_institucional"] = ("responsable_ur_institucional", "Responsable UR con consulta institucional"),
        ["responsable_supervisor"] = ("responsable_ur_supervisor", "Responsable de UR con supervisión"),
        ["local"] = ("colaborador_local", "Colaborador local"),
        ["dependencias"] = ("colaborador_dependencias", "Colaborador de áreas dependientes"),
        ["consulta"] = ("consulta_institucional", "Consulta institucional"),
        ["administrador"] = ("administrador", "Administrador")
    };
    public static string Label(string kind) => All.GetValueOrDefault(kind).Label ?? "Escenario de rol y área";
    public static Profile Resolve(AccessSelection selection, UnitDirectory directory, Profile real)
    {
        var unit = directory.Get(selection.UnitId);
        if (!All.TryGetValue(selection.Role ?? "", out var role) || unit is null || !directory.Participation.EligibleType(unit)
            || selection.Role is "responsable" or "responsable_institucional" or "responsable_supervisor" && !(UnitDirectory.IsForm(unit) || directory.Participates(unit))
            || selection.Role is "local" or "dependencias" && directory.LevelTwo(unit.Id) is null)
            throw new DomainProblem(422, "Selecciona un rol y un área activa válidos para la vista de prueba.", new { errors = new { ur = "El área no admite este escenario." } });
        return new(new("", "Vista de prueba", "vista-de-prueba", unit.Id), [new(0, role.Key, role.Label)], real.Modules);
    }
}
