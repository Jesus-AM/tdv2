using System.Text.Json.Nodes;
namespace Tdv2.Domain;

public sealed record ToolSuite(string Tool, string Field, string Label, IReadOnlyDictionary<string, string> Options);

/// <summary>Claves de producto estables; cada familia tiene su colección independiente.</summary>
public static class SystemSuites
{
    public static readonly ToolSuite Microsoft = new("microsoft_365", "aplicacionesMicrosoft365", "Aplicaciones de Microsoft 365",
        new Dictionary<string, string> { ["excel"] = "Excel", ["word"] = "Word", ["powerpoint"] = "PowerPoint",
            ["outlook"] = "Outlook — correo institucional", ["teams"] = "Teams", ["forms"] = "Forms — formularios",
            ["onedrive"] = "OneDrive", ["sharepoint"] = "SharePoint" });
    public static readonly ToolSuite Adobe = new("adobe_creative_cloud", "aplicacionesAdobe", "Aplicaciones de Adobe Creative Cloud",
        new Dictionary<string, string> { ["acrobat_pro"] = "Acrobat Pro", ["acrobat_reader"] = "Acrobat Reader",
            ["adobe_express"] = "Adobe Express", ["adobe_scan"] = "Adobe Scan", ["after_effects"] = "After Effects",
            ["animate"] = "Animate", ["audition"] = "Audition", ["bridge"] = "Bridge", ["character_animator"] = "Character Animator",
            ["dreamweaver"] = "Dreamweaver", ["fresco"] = "Fresco", ["illustrator"] = "Illustrator", ["incopy"] = "InCopy",
            ["indesign"] = "InDesign", ["lightroom"] = "Lightroom", ["lightroom_classic"] = "Lightroom Classic",
            ["media_encoder"] = "Media Encoder", ["photoshop"] = "Photoshop", ["photoshop_express"] = "Photoshop Express", ["premiere"] = "Premiere" });
    public static readonly ToolSuite[] All = [Microsoft, Adobe];
    public static readonly string[] Fields = All.Select(s => s.Field).ToArray();
    public static ToolSuite? ForTool(string? tool) => All.FirstOrDefault(s => s.Tool == tool);
    private static bool Valid(JsonNode? node, ToolSuite suite) => node is JsonArray values && values.Count <= suite.Options.Count
        && values.All(v => v is JsonValue value && value.TryGetValue<string>(out var key) && suite.Options.ContainsKey(key))
        && values.Select(v => v!.ToString()).Distinct(StringComparer.Ordinal).Count() == values.Count;
    public static bool Complete(JsonNode? row, ToolSuite suite) => row?[suite.Field] is JsonArray { Count: > 0 } values && Valid(values, suite);
    public static JsonArray Normalize(JsonNode? node, string block, ToolSuite suite)
    {
        if (!Valid(node, suite)) throw FormSchema.FieldError(block, suite.Field,
            $"Selecciona opciones válidas en {suite.Label}, sin repeticiones y de la familia correspondiente.");
        return node!.DeepClone().AsArray();
    }
}
