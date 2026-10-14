using System.Text.Json.Nodes;
namespace Tdv2.Domain;

/// <summary>Versión del contrato JSON; independiente de las versiones de concurrencia del formato/bloques.</summary>
public static class FormDocument
{
    public const string Field = "versionEstructura";
    public const int Current = 1;
    // Los documentos anteriores son versión 0. Consultarlos nunca los transforma ni escribe.
    public static int Version(JsonObject content)
    {
        if (!content.ContainsKey(Field)) return 0;
        if (content[Field] is JsonValue value && value.TryGetValue<int>(out var version) && version is >= 0 and <= Current)
            return version;
        throw new DomainProblem(409, "La estructura de este formato requiere una versión compatible de TDV2. Sus respuestas se conservan.");
    }
    public static JsonObject Read(string text)
    {
        var content = JsonNode.Parse(text)!.AsObject(); Version(content); return content;
    }
    public static void Stamp(JsonObject content) { Version(content); content[Field] = Current; }
}
