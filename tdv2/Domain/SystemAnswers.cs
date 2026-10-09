using System.Text.Json.Nodes;
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
        bool Has(string key) => !string.IsNullOrWhiteSpace(row?[key]?.ToString());
        if (!Has(field)) return false;
        return field switch
        {
            "sistema" when row?[field]?.ToString() == "sii_v2" => Has("moduloSiiId") && Has("moduloSiiDescripcion"),
            "sistema" when row?[field]?.ToString() == "otra" => Has("sistemaOtro"),
            "uso" when row?[field]?.ToString() == "otro" => Has("usoOtro"),
            _ => true // Las respuestas históricas se leen sin convertirlas ni borrarlas.
        };
    }
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
