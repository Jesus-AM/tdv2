using System.Text.Json.Nodes;

namespace Tdv2.Domain;

/// <summary>Selecciones y descripciones históricas dentro del JSON del formato.</summary>
public static class SystemModules
{
    public const string Field = "modulosSii";

    // La colección, incluso vacía, prevalece sobre la asociación escalar antigua.
    public static JsonArray Read(JsonNode? row)
    {
        if (row is JsonObject obj && obj.ContainsKey(Field))
            return obj[Field] as JsonArray ?? new JsonArray((JsonNode?)null);
        var id = row?["moduloSiiId"]?.ToString();
        return string.IsNullOrEmpty(id) ? new JsonArray() : new JsonArray(new JsonObject
        { ["id"] = id, ["descripcion"] = row?["moduloSiiDescripcion"]?.DeepClone() ?? JsonValue.Create("") });
    }

    public static bool Complete(JsonNode? row)
    {
        var modules = Read(row);
        return modules.Count > 0 && ValidIds(modules) && modules.All(m => SystemAnswers.ValidText(m, "descripcion"));
    }

    private static bool ValidIds(JsonArray modules) => modules.All(m => m is JsonObject obj
        && obj["id"] is JsonValue value && value.TryGetValue<string>(out var id) && SystemAnswers.ValidModuleId(id))
        && modules.Select(m => m!["id"]!.ToString()).Distinct(StringComparer.Ordinal).Count() == modules.Count;

    public static JsonArray Normalize(JsonNode? value, string block)
    {
        if (value is not JsonArray modules || !ValidIds(modules)
            || modules.Any(m => m!.AsObject().Any(p => p.Key is not ("id" or "descripcion"))
                || m["descripcion"] is not JsonValue text || !text.TryGetValue<string>(out var label) || label.EnumerateRunes().Count() > 4000))
            throw FormSchema.FieldError(block, Field, "Selecciona módulos de SIIv2 válidos, sin repeticiones.");
        return modules.DeepClone().AsArray();
    }

    // El proveedor consulta y bloquea el catálogo en la misma transacción de la escritura.
    // Nunca confiar en descripciones enviadas por el cliente ni refrescar instantáneas conservadas.
    public static async Task Confirm(JsonObject row, JsonNode? previous, Func<string, Task<string?>> availableDescription)
    {
        // Los detalles dormidos se conservan; al volver a SIIv2 se verifica el catálogo.
        if (row["sistema"]?.ToString() != "sii_v2") return;
        var selection = Read(row); var old = Read(previous); var block = "sistemas:" + row["id"];
        if (JsonNode.DeepEquals(selection, old)
            && !(row["sistema"]?.ToString() == "sii_v2" && previous?["sistema"]?.ToString() != "sii_v2")) return;
        Normalize(selection, block);
        var confirmed = new JsonArray();
        foreach (var item in selection)
        {
            var id = item!["id"]!.ToString();
            var kept = old.FirstOrDefault(m => m?["id"]?.ToString() == id);
            // Una referencia retirada se conserva si ya estaba seleccionada; no permite nuevas selecciones.
            var description = kept is not null && previous?["sistema"]?.ToString() == "sii_v2"
                ? kept["descripcion"]?.ToString() ?? "" : await availableDescription(id);
            if (description is null)
                throw FormSchema.FieldError(block, Field, "Selecciona módulos disponibles del catálogo local de SIIv2. Se conservan los módulos históricos ya registrados.");
            confirmed.Add(new JsonObject { ["id"] = id, ["descripcion"] = description });
        }
        if (row.ContainsKey(Field))
        {
            row[Field] = confirmed;
            row["moduloSiiId"] = ""; row["moduloSiiDescripcion"] = "";
        }
        else row["moduloSiiDescripcion"] = confirmed.FirstOrDefault()?["descripcion"]?.DeepClone() ?? JsonValue.Create("");
    }
}
