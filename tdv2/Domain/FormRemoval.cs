using System.Text.Json.Nodes;
namespace Tdv2.Domain;

public sealed record RowRemoval(string Section, string Id);
public sealed record EditRejection(int Status, string Code, string Message);
/// <summary>El servidor determina el retiro y sus relaciones; una reserva auxiliar no concede captura libre.</summary>
public static class FormRemoval
{
    public static void Validate(RowRemoval target)
    {
        if (target.Section is not ("identificacion" or "sistemas" or "datos" or "acuerdos") || string.IsNullOrEmpty(target.Id)
            || !FormBlocks.ValidKey(target.Section + ":" + target.Id))
            throw new DomainProblem(422, "El registro que deseas retirar no es válido.");
    }
    public static bool Exists(JsonObject content, RowRemoval target) => content[target.Section]!.AsArray().Any(r => r?["id"]?.ToString() == target.Id);
    public static Dictionary<string, JsonNode?> Changes(JsonObject content, RowRemoval target)
    {
        Validate(target);
        var row = content[target.Section]!.AsArray().FirstOrDefault(r => r?["id"]?.ToString() == target.Id)
            ?? throw new DomainProblem(409, "El registro ya no está disponible. Revisa el contenido vigente.");
        var result = new Dictionary<string, JsonNode?>(StringComparer.Ordinal) { [target.Section + ":" + target.Id] = null };
        if (target.Section != "identificacion" || row["codigo"]?.ToString() is not { Length: > 0 } code) return result;
        if (content["evaluaciones"]?[code] is JsonArray evaluations)
            for (var i = 0; i < evaluations.Count; i++) result[$"evaluaciones:{code}:{i}"] = null;
        foreach (var section in new[] { "sistemas", "datos" })
            foreach (var linked in content[section]!.AsArray().Where(r => r?["proceso"]?.ToString() == code))
            {
                var value = linked!.DeepClone(); value["proceso"] = "";
                result[section + ":" + linked["id"]] = value;
            }
        return result;
    }
}
