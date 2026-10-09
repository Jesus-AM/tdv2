using System.Text.Json.Nodes;
namespace Tdv2.Domain;

/// <summary>La primera etapa limita captura, no altera participación, permisos de UR ni respuestas históricas.</summary>
public static class FormCapture
{
    public static bool CurrentYear(int? year, Unit unit) => year is null or 0 || year == unit.Year;
    public static bool LaterSections(Profile profile) => profile.Has("administrador");
    public static bool FirstStageBlock(string key) => key is "contexto" or "medios"
        || key.StartsWith("identificacion:", StringComparison.Ordinal) || key.StartsWith("sistemas:", StringComparison.Ordinal);
    public static string Section(Profile profile, string? section) => section is "contexto" or "identificacion" or "sistemas"
        || LaterSections(profile) && FormBlocks.Sections.Contains(section) ? section! : "contexto";
    public static void Require(Profile profile, IEnumerable<string> keys)
    {
        if (!LaterSections(profile) && keys.Any(k => !FirstStageBlock(k)))
            throw new DomainProblem(403, "Esta sección no está habilitada en la primera etapa de captura.");
    }
    public static FormReview Review(JsonObject content) => FormReview.Inspect(content, firstStage: true);
    public static int Progress(JsonObject content)
    {
        // Contexto es informativo; medios y observaciones siguen siendo opcionales. No se fabrican respuestas.
        var rows = content["identificacion"]!.AsArray(); var systems = content["sistemas"]!.AsArray();
        var total = Math.Max(1, rows.Count) * 7 + Math.Max(1, systems.Count) * 4;
        var complete = rows.Sum(r => ProcedureEligibility.Required.Count(f => ProcedureEligibility.Valid(r, f.Key)));
        complete += systems.Sum(r => new[] { "sistema", "uso", "estado" }.Count(k => SystemAnswers.Filled(r, k))
            + (ProcedureEligibility.Contains(content, r?["proceso"]?.ToString()) ? 1 : 0));
        return (int)Math.Floor(100d * complete / total);
    }
}
