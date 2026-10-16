using System.Text.Json.Nodes;
namespace Tdv2.Domain;

/// <summary>La primera etapa limita captura, no altera participación, permisos de UR ni respuestas históricas.</summary>
public static class FormCapture
{
    public static bool CurrentYear(int? year, Unit unit) => year is null or 0 || year == unit.Year;
    public static bool LaterSections(Profile profile) => profile.Has("administrador");
    public static bool FirstStageBlock(string key) => key is "contexto"
        || key.StartsWith("identificacion:", StringComparison.Ordinal) || key.StartsWith("sistemas:", StringComparison.Ordinal);
    public static string Section(Profile profile, string? section) => section is "contexto" or "identificacion" or "sistemas" or "revision"
        || LaterSections(profile) && FormBlocks.Sections.Contains(section) ? section! : "contexto";
    public static void Require(Profile profile, IEnumerable<string> keys)
    {
        if (!LaterSections(profile) && keys.Any(k => !FirstStageBlock(k)))
            throw new DomainProblem(403, "Esta sección no está habilitada en la primera etapa de captura.");
    }
    public static FormReview Review(JsonObject content) => FormReview.Inspect(content, firstStage: true);
    public static int Progress(JsonObject content) => FormRequirements.Inspect(content).Percentage;
}
