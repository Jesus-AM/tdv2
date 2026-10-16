using System.Text.Json.Nodes;
using System.Globalization;

namespace Tdv2.Domain;

public sealed record ApplicationKind(string Key, string Tool, string Field, string Label);

/// <summary>Cada colección conserva su categoría, ID estable (local o anterior) e instantánea autorizada.</summary>
public static class SystemApplications
{
    public static readonly ApplicationKind Mobile = new("moviles", "aplicaciones_moviles", "aplicacionesMoviles", "Aplicaciones móviles");
    public static readonly ApplicationKind Web = new("web", "aplicaciones_web", "aplicacionesWeb", "Aplicaciones Web");
    public static readonly ApplicationKind[] Kinds = [Mobile, Web];
    public static readonly string[] Fields = Kinds.Select(k => k.Field).ToArray();
    public static ApplicationKind? ForTool(string? tool) => Kinds.FirstOrDefault(k => k.Tool == tool);
    public static JsonArray Read(JsonNode? row, ApplicationKind kind) => row is JsonObject obj && obj.ContainsKey(kind.Field)
        ? obj[kind.Field] as JsonArray ?? new JsonArray((JsonNode?)null) : [];
    public static bool LocalId(string text) => text.StartsWith("local:", StringComparison.Ordinal)
        && Guid.TryParseExact(text[6..], "D", out var id) && text == "local:" + id.ToString("D");
    public static bool ValidId(string text) => LocalId(text) || int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var id)
        && id.ToString(CultureInfo.InvariantCulture) == text;
    private static bool ValidIds(JsonArray values, ApplicationKind kind) => values.All(v => v is JsonObject obj
        && obj["id"] is JsonValue id && id.TryGetValue<string>(out var text) && ValidId(text)
        && obj["catalogo"] is JsonValue catalog && catalog.TryGetValue<string>(out var key) && key == kind.Key)
        && values.Select(v => v!["id"]!.ToString()).Distinct(StringComparer.Ordinal).Count() == values.Count;
    public static bool Complete(JsonNode? row, ApplicationKind kind)
    {
        var values = Read(row, kind);
        return values.Count > 0 && ValidIds(values, kind) && values.All(v => SystemAnswers.ValidText(v, "descripcion"));
    }
    public static JsonArray Normalize(JsonNode? value, string block, ApplicationKind kind)
    {
        if (value is not JsonArray values || !ValidIds(values, kind)
            || values.Any(v => v!.AsObject().Any(p => p.Key is not ("id" or "descripcion" or "catalogo"))
                || v["descripcion"] is not JsonValue text || !text.TryGetValue<string>(out var label) || label.EnumerateRunes().Count() > 4000))
            throw FormSchema.FieldError(block, kind.Field, $"Selecciona {kind.Label.ToLowerInvariant()} del catálogo correspondiente, sin repeticiones.");
        return values.DeepClone().AsArray();
    }
    public static async Task Confirm(JsonObject row, JsonNode? previous, ApplicationKind kind, Func<string, Task<string?>> availableDescription)
    {
        // Al alternar herramienta se conservan detalles dormidos, nunca requisitos activos de otro catálogo.
        if (row["sistema"]?.ToString() != kind.Tool) return;
        var selection = Read(row, kind); var old = Read(previous, kind);
        if (JsonNode.DeepEquals(selection, old) && previous?["sistema"]?.ToString() == kind.Tool) return;
        Normalize(selection, "sistemas:" + row["id"], kind);
        var confirmed = new JsonArray();
        foreach (var item in selection)
        {
            var id = item!["id"]!.ToString();
            var kept = old.FirstOrDefault(v => v?["id"]?.ToString() == id && v?["catalogo"]?.ToString() == kind.Key);
            var description = kept is not null && previous?["sistema"]?.ToString() == kind.Tool
                ? kept["descripcion"]?.ToString() ?? "" : await availableDescription(id);
            if (description is null) throw FormSchema.FieldError("sistemas:" + row["id"], kind.Field,
                $"Selecciona {kind.Label.ToLowerInvariant()} disponibles del catálogo local. Se conservan las referencias históricas ya registradas.");
            confirmed.Add(new JsonObject { ["id"] = id, ["catalogo"] = kind.Key, ["descripcion"] = description });
        }
        row[kind.Field] = confirmed;
    }
}
