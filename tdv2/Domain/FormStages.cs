using System.Text.Json.Nodes;
using Tdv2.Domain.Entities;
namespace Tdv2.Domain;

public sealed record FormStageStatus(int id, string nombre, bool habilitada, string estado, int ejercicio,
    DateTimeOffset? enviadoEn, string? enviadoPor, string? enviadoNombre)
{
    public bool Blocked => !habilitada || estado is "enviada" or "historico";
}
public sealed record FormSectionReview(string id, string nombre, bool completa);
public sealed record FormStageSummary(int procedimientos, int herramientas, string[] medios);
public sealed record FormStageReview(FormStageStatus etapa, FormReview revision,
    FormSectionReview[] secciones, FormStageSummary resumen);
public sealed record FormStageDefinition(int Id, string Name, string[] ContentKeys, Func<string, bool> IncludesBlock,
    Func<JsonObject, FormReview> Review, Func<JsonObject, int> Progress,
    (string Id, string Name)[] Sections, Func<JsonObject, FormStageSummary> Summarize);

/// <summary>Única etapa habilitada. Una futura etapa requiere su propia definición, nunca hereda el envío anterior.</summary>
public static class FormStages
{
    public const int First = 1;
    private static readonly IReadOnlyDictionary<int, FormStageDefinition> Definitions = new Dictionary<int, FormStageDefinition>
    {
        [First] = new(First, "Primera etapa", ["encabezado", "identificacion", "sistemas", "medios", "medioOtro"],
            FormCapture.FirstStageBlock, FormCapture.Review, FormCapture.Progress,
            [("contexto", "Contexto"), ("identificacion", "Identificación general"), ("sistemas", "Sistemas y herramientas, incluidos Medios utilizados")],
            FirstSummary)
    };
    public static bool Enabled(int stage) => Definitions.ContainsKey(stage);
    public static string Name(int stage) => Definitions.GetValueOrDefault(stage)?.Name ?? $"Etapa {stage}";
    public static FormStageDefinition Definition(int stage) { RequireEnabled(stage); return Definitions[stage]; }
    public static int Progress(int stage, JsonObject content) => Definitions.GetValueOrDefault(stage)?.Progress(content) ?? 0;
    public static FormStageSubmission? Submission(UnitForm? form) => form?.StageSubmissions.SingleOrDefault(s => s.Stage == form.ActiveStage);
    public static FormStageStatus Status(UnitForm? form, int year = 0)
    {
        var id = form?.ActiveStage ?? First; var sent = Submission(form); var legacy = form?.SubmittedAt is not null;
        return new(id, Name(id), Enabled(id), legacy ? "historico" : sent is not null ? "enviada" : "borrador",
            form?.Year is > 0 ? form.Year : year, legacy ? form!.SubmittedAt : sent?.SubmittedAt,
            legacy ? form!.SubmittedEffective ?? form.SubmittedBy : sent?.Effective, legacy ? null : sent?.Name);
    }
    public static void RequireEnabled(int stage)
    {
        if (!Enabled(stage)) throw new DomainProblem(409, "La etapa solicitada todavía no está habilitada.");
    }
    public static void RequireWritable(UnitForm form, IEnumerable<string> blocks)
    {
        var frozen = form.StageSubmissions.SelectMany(s =>
            (JsonNode.Parse(s.Snapshot)?["contenido"] as JsonObject)?.Select(p => p.Key) ?? []).ToHashSet(StringComparer.Ordinal);
        if (blocks.Any(key => frozen.Contains(key == "contexto" ? "encabezado" : key.Split(':')[0])))
            throw new DomainProblem(409, "Las respuestas de una etapa enviada permanecen bloqueadas.");
    }
    public static JsonObject Capture(JsonObject content, int stage)
    {
        return new JsonObject(Definition(stage).ContentKeys
            .Select(key => KeyValuePair.Create(key, content[key]?.DeepClone())));
    }
    public static FormStageReview Describe(FormStageStatus stage, JsonObject content)
    {
        // No se fabrica una revisión de etapas sin requisitos definidos.
        var definition = Definitions.GetValueOrDefault(stage.id);
        var review = definition?.Review(content) ?? new FormReview(false, []);
        var sections = definition?.Sections
            .Select(s => new FormSectionReview(s.Id, s.Name, review.pendientes.All(p => p.seccion != s.Id))).ToArray() ?? [];
        return new(stage, review, sections, definition?.Summarize(content) ?? new(0, 0, []));
    }
    private static FormStageSummary FirstSummary(JsonObject content)
    {
        var media = (content["medios"] as JsonObject)?.Where(p => p.Value is JsonValue v && v.TryGetValue<bool>(out var selected) && selected)
            .Select(p => p.Key).ToList() ?? [];
        if (!string.IsNullOrWhiteSpace(content["medioOtro"]?.ToString())) media.Add(content["medioOtro"]!.ToString());
        return new((content["identificacion"] as JsonArray)?.Count ?? 0, (content["sistemas"] as JsonArray)?.Count ?? 0, media.ToArray());
    }
}
