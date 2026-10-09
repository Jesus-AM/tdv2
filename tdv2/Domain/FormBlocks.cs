using System.Text.Json.Nodes;
namespace Tdv2.Domain;

/// <summary>Claves estables por fila; las posiciones visuales nunca identifican filas compartidas.</summary>
public static class FormBlocks
{
    public static readonly string[] Sections = ["contexto", "identificacion", "sistemas", "datos", "evaluacion", "preguntas", "acuerdos"];
    public static readonly string[] Locations = [..Sections, "revision"];
    private static readonly string[] Tables = ["identificacion", "sistemas", "datos", "acuerdos"];
    public static Dictionary<string, JsonNode?> Split(JsonObject content)
    {
        var blocks = new Dictionary<string, JsonNode?>(StringComparer.Ordinal)
        {
            ["contexto"] = content["encabezado"]?.DeepClone(),
            ["medios"] = new JsonObject { ["medios"] = content["medios"]?.DeepClone(), ["medioOtro"] = content["medioOtro"]?.DeepClone() }
        };
        foreach (var table in Tables)
            foreach (var row in content[table]!.AsArray()) blocks[table + ":" + row!["id"]!.ToString()] = row.DeepClone();
        for (var i = 0; i < content["preguntas"]!.AsArray().Count; i++) blocks["preguntas:" + i] = content["preguntas"]![i]!.DeepClone();
        if (content["evaluaciones"] is JsonObject evaluations)
            foreach (var (code, rows) in evaluations)
                for (var i = 0; i < rows!.AsArray().Count; i++) blocks[$"evaluaciones:{code}:{i}"] = rows[i]!.DeepClone();
        return blocks;
    }
    public static bool ValidKey(string key) => key.Length <= 512 && (key is "contexto" or "medios"
        || Tables.Any(t => key.StartsWith(t + ":", StringComparison.Ordinal) && key.Length > t.Length + 1 && key.Length <= t.Length + 65)
        || System.Text.RegularExpressions.Regex.IsMatch(key, @"\A(preguntas:(?:[0-9]|1[01])|evaluaciones:PO-[0-9]{2,6}:[0-8])\z"));

    public static JsonObject Apply(JsonObject original, IReadOnlyDictionary<string, JsonNode?> changes)
    {
        var result = original.DeepClone().AsObject();
        foreach (var (key, value) in changes)
        {
            if (!ValidKey(key)) throw new DomainProblem(422, "El bloque solicitado no es válido.");
            if (key == "contexto") result["encabezado"] = value?.DeepClone();
            else if (key == "medios")
            {
                result["medios"] = value?["medios"]?.DeepClone(); result["medioOtro"] = value?["medioOtro"]?.DeepClone();
            }
            else if (key.StartsWith("preguntas:")) result["preguntas"]![int.Parse(key[10..])] = value?.DeepClone();
            else if (key.StartsWith("evaluaciones:"))
            {
                var parts = key.Split(':'); var code = parts[1]; var index = int.Parse(parts[2]);
                if (result["evaluaciones"] is not JsonObject) result["evaluaciones"] = new JsonObject();
                if (result["evaluaciones"]![code] is not JsonArray) result["evaluaciones"]![code] = new JsonArray(Enumerable.Repeat<JsonNode?>(null, 9).ToArray());
                result["evaluaciones"]![code]![index] = value?.DeepClone();
            }
            else
            {
                var separator = key.IndexOf(':'); var table = key[..separator]; var id = key[(separator + 1)..];
                var rows = result[table]!.AsArray(); var index = rows.Select((r, i) => (r, i)).Where(p => p.r!["id"]!.ToString() == id).Select(p => p.i).DefaultIfEmpty(-1).Single();
                if (value is null)
                {
                    if (index >= 0) rows.RemoveAt(index);
                }
                else
                {
                    if (value is not JsonObject || value["id"]?.ToString() != id) throw new DomainProblem(422, "El identificador del bloque cambió.");
                    if (table == "identificacion" && index >= 0 && rows[index]?["codigo"]?.ToString() is { Length: > 0 } assigned
                        && value["codigo"]?.ToString() != assigned)
                        throw FormSchema.FieldError(key, "codigo", "El código confirmado del proceso no puede cambiarse.");
                    if (table == "identificacion" && value["prioridad"]?.ToString() is { Length: > 0 } priority
                        && priority is not ("1" or "2" or "3" or "4" or "5")
                        && (index < 0 || rows[index]?["prioridad"]?.ToString() != priority))
                        throw FormSchema.FieldError(key, "prioridad", "Selecciona una prioridad de 1 a 5.");
                    if (index < 0) rows.Add(value.DeepClone()); else rows[index] = value.DeepClone();
                }
            }
        }
        if (result["evaluaciones"] is JsonObject evaluations)
            foreach (var (key, rows) in evaluations.ToArray())
                if (rows!.AsArray().All(r => r is null)) evaluations.Remove(key);
        return result;
    }
}
