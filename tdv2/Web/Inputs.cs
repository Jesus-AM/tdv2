using System.Text.Json.Nodes;
using Tdv2.Domain;
namespace Tdv2.Web;

public static class Inputs
{
    public static DomainProblem Invalid(string field, string message) => new(422, message, new { errors = new Dictionary<string, string> { [field] = message } });
    public static string Text(JsonObject input, string key, int min, int max)
    {
        if (input[key] is not JsonValue value || !value.TryGetValue<string>(out var text) || text.Trim().Length < min || text.Length > max)
            throw Invalid(key, "Revisa el campo " + key + ".");
        return text.Trim();
    }
    public static bool Bool(JsonObject input, string key) => input[key] is JsonValue v && v.TryGetValue<bool>(out var b) ? b : throw Invalid(key, "Selecciona una opción válida.");
    public static string Query(HttpContext http, string key, int min, int max)
    {
        var value = http.Request.Query[key];
        if (value.Count != 1 || value.ToString().Trim().Length < min || value.ToString().Length > max) throw Invalid(key, "Revisa la búsqueda y el área seleccionada.");
        return value.ToString().Trim();
    }
    public static int Page(HttpContext http, string key)
    {
        if (!http.Request.Query.ContainsKey(key)) return 1;
        var raw = Query(http, key, 1, 5);
        return int.TryParse(raw, out var page) && page is >= 1 and <= 10000 ? page : throw Invalid(key, "La página no es válida.");
    }
}
