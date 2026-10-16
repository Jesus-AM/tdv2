using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
namespace Tdv2.Domain;

public static class SystemUniqueness
{
    public const string Message = "Esta herramienta ya está agregada a este procedimiento. Usa la fila existente o elige otra herramienta o procedimiento. Tus respuestas se conservan.";
    public static string OtherKey(string text) => Regex.Replace(text.Trim(), @"\s+", " ").ToUpperInvariant();
    private static Dictionary<string, (string Procedure, string Tool, string Other)> Keys(JsonObject? content)
    {
        var result = new Dictionary<string, (string, string, string)>(StringComparer.Ordinal);
        if (content is null) return result;
        var procedures = (content["identificacion"] as JsonArray ?? []).OfType<JsonObject>()
            .Where(p => !string.IsNullOrEmpty(p["codigo"]?.ToString())).GroupBy(p => p["codigo"]!.ToString())
            .ToDictionary(g => g.Key, g => g.First()["id"]!.ToString(), StringComparer.Ordinal);
        foreach (var row in (content["sistemas"] as JsonArray ?? []).OfType<JsonObject>())
        {
            var tool = row["sistema"]?.ToString() ?? "";
            if (tool.Length == 0 || !procedures.TryGetValue(row["proceso"]?.ToString() ?? "", out var procedure)) continue;
            var other = tool == "otra" ? OtherKey(row["sistemaOtro"]?.ToString() ?? "") : "";
            if (tool == "otra" && other.Length == 0) continue;
            result[row["id"]!.ToString()] = (procedure, tool, other);
        }
        return result;
    }
    // Bajo el bloqueo de UR: una pareja histórica puede conservarse, pero ninguna pareja nueva se admite.
    // Comparar parejas también impide reemplazar un duplicado por otra fila manteniendo sólo la cantidad.
    public static void Validate(JsonObject content, JsonObject? previous)
    {
        var before = Keys(previous); var after = Keys(content);
        foreach (var group in after.GroupBy(p => p.Value).Where(g => g.Count() > 1))
        {
            var rows = group.ToArray();
            for (var i = 0; i < rows.Length; i++) for (var j = i + 1; j < rows.Length; j++)
            {
                if (before.TryGetValue(rows[i].Key, out var a) && before.TryGetValue(rows[j].Key, out var b)
                    && a == group.Key && b == group.Key) continue;
                var changed = before.GetValueOrDefault(rows[j].Key) != group.Key ? rows[j] : rows[i];
                var field = before.TryGetValue(changed.Key, out var old) && old.Procedure != group.Key.Procedure ? "proceso"
                    : group.Key.Tool == "otra" ? "sistemaOtro" : "sistema";
                throw FormSchema.FieldError("sistemas:" + changed.Key, field, Message);
            }
        }
    }
}
