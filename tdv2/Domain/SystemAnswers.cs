using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace Tdv2.Domain;

public static class SystemAnswers
{
    public static readonly string[] Tools = ["sii_v2", "portal_cast", "excel", "correo", "formulario", "papel", "otra"];
    public static readonly string[] Uses = ["registrar", "agendar", "consultar", "seguimiento", "avisos", "reportes", "autorizar", "otro"];
    public static readonly string[] States = ["bien", "fallas", "no_funciona", "sin_uso"];
    public static readonly string[] Details = ["sistemaOtro", "usoOtro", "moduloSiiId", "moduloSiiDescripcion"];
    public static string ToolLabel(JsonNode? row) => row?["sistema"]?.ToString() switch
    {
        "sii_v2" => "SIIv2", "portal_cast" => "Portal del CAST de uso de técnicos", "excel" => "Hoja de cálculo (Excel)",
        "correo" => "Correo electrónico institucional", "formulario" => "Formulario en línea", "papel" => "Formato impreso (papel)",
        "otra" => string.IsNullOrWhiteSpace(row?["sistemaOtro"]?.ToString()) ? "Otra herramienta" : row!["sistemaOtro"]!.ToString(),
        null or "" => "Registro sin completar", var historical => historical
    };
    public static bool Filled(JsonNode? row, string field)
    {
        return field switch
        {
            "sistema" when row?[field]?.ToString() == "sii_v2" => ValidModuleId(row?["moduloSiiId"]?.ToString()) && ValidText(row, "moduloSiiDescripcion"),
            "sistema" when row?[field]?.ToString() == "otra" => ValidText(row, "sistemaOtro"),
            "uso" when row?[field]?.ToString() == "otro" => ValidText(row, "usoOtro"),
            "sistema" => Tools.Contains(row?[field]?.ToString()),
            "uso" => Uses.Contains(row?[field]?.ToString()),
            "estado" => States.Contains(row?[field]?.ToString()),
            _ => ValidText(row, field)
        };
    }
    public static bool ValidText(JsonNode? row, string field) => row?[field] is JsonValue value && value.TryGetValue<string>(out var text)
        && !string.IsNullOrWhiteSpace(text) && text.EnumerateRunes().Count() <= 4000;
    public static bool ValidModuleId(string? id) => id is not null && Regex.IsMatch(id, @"\A[0-9]{1,40}\z") && id.Any(c => c != '0');
    public static void Validate(JsonObject row, JsonNode? previous, string block)
    {
        foreach (var (field, choices) in new[] { ("sistema", Tools), ("uso", Uses), ("estado", States) })
        {
            var value = row[field]?.ToString() ?? "";
            if (value != "" && value != previous?[field]?.ToString() && !choices.Contains(value, StringComparer.Ordinal))
                throw FormSchema.FieldError(block, field, "Selecciona una opción de la lista. La respuesta histórica puede conservarse sin cambios.");
        }
    }
}
