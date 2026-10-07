namespace Tdv2.Domain;

/// <summary>Participar crea una opción de formato, nunca una autorización institucional.</summary>
public sealed record Participation(int Version, int[] Levels, string[] ExcludedTypes)
{
    public static Participation Default => new(1, [2, 3], ["N"]);
    public static string Normalize(string? kind) => (kind ?? "").Trim().ToUpperInvariant();
    public bool EligibleType(Unit unit) => !ExcludedTypes.Contains(Normalize(unit.Kind), StringComparer.Ordinal);
    public bool Includes(Unit unit) => unit.LevelKnown && Levels.Contains(unit.Level) && EligibleType(unit);
}
