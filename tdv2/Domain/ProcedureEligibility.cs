using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace Tdv2.Domain;

/// <summary>Regla única para procedimientos persistidos del mismo formato. No exige respuestas nuevas.</summary>
public static class ProcedureEligibility
{
    public static readonly (string Key, string Label)[] Required = [
        ("tramite", "Trámite o servicio"), ("usuario", "¿A quién atiende?"), ("resultado", "¿Qué entrega?"),
        ("responsable", "Área responsable"), ("validacion", "validación"), ("prioridad", "prioridad"), ("codigo", "código mediante validación")];
    public static bool Valid(JsonNode? row, string field) => field switch
    {
        "usuario" => FormSchema.HasRecipients(row),
        "prioridad" => row?[field]?.ToString() is "1" or "2" or "3" or "4" or "5",
        "validacion" => row?[field]?.ToString() is "V" or "A" or "D" or "N",
        "codigo" => Regex.IsMatch(row?[field]?.ToString() ?? "", @"\APO-[0-9]{2,6}\z"),
        _ => row?[field] is JsonValue value && value.TryGetValue<string>(out var text)
            && !string.IsNullOrWhiteSpace(text) && text.EnumerateRunes().Count() <= 4000
    };
    public static bool Available(JsonNode? row) => row?["id"]?.ToString() is { Length: > 0 }
        && row?["validacion"]?.ToString() is "V" or "A" && Required.All(f => Valid(row, f.Key));
    public static bool Contains(JsonObject content, string? code) => !string.IsNullOrEmpty(code)
        && content["identificacion"]!.AsArray().Any(r => r?["codigo"]?.ToString() == code && Available(r));
    // Sólo se llama con el contenido confirmado por PostgreSQL, nunca con propuestas del navegador.
    public static object[] Options(JsonObject content) => content["identificacion"]!.AsArray().Where(Available)
        .Select(r => (object)new { id = r!["id"]!.ToString(), codigo = r["codigo"]!.ToString(), tramite = r["tramite"]!.ToString() }).ToArray();
    public const string Hint = "Completa y valida los procedimientos en Identificación general para seleccionarlos aquí";
}
