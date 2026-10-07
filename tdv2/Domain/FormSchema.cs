using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Tdv2.Domain;

public sealed class FormSchema
{
    private readonly JsonObject definition;
    public FormSchema(string json) => definition = JsonNode.Parse(json)!.AsObject();
    public JsonObject Definition() => (JsonObject)definition.DeepClone();
    public static readonly string[] UserChoices = ["Comunidad universitaria", "Docentes", "Estudiantes", "Personal administrativo", "Otro"];
    public static bool HasUsers(JsonNode? node) => node is JsonArray { Count: > 0 } users
        && users.All(v => v is JsonValue value && value.TryGetValue<string>(out var text) && UserChoices.Contains(text, StringComparer.Ordinal))
        && users.Select(v => v!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count() == users.Count;
    private static JsonArray Users(JsonNode? node)
    {
        if (node is not JsonArray users || users.Count > UserChoices.Length || users.Count > 0 && !HasUsers(users))
            throw Invalid("Selecciona usuarios que atiende válidos, sin opciones repetidas.");
        return users.DeepClone().AsArray();
    }
    private static readonly Dictionary<string, string[]> Fields = new()
    {
        ["identificacion"] = ["id", "codigo", "prioridad", "fuente", "area", "tramite", "usuario", "resultado", "responsable", "validacion"],
        ["sistemas"] = ["id", "proceso", "sistema", "uso", "estado", "fallas"],
        ["datos"] = ["id", "proceso", "dato", "fuente", "origen", "detalle"],
        ["acuerdos"] = ["id", "acuerdo", "responsable", "fecha"]
    };
    public JsonObject Blank(Unit unit)
    {
        var result = new JsonObject
        {
            ["encabezado"] = new JsonObject { ["fecha"] = "", ["area"] = "", ["responsable"] = "" },
            ["medioOtro"] = "",
            ["evaluaciones"] = new JsonObject(),
            ["medios"] = new JsonObject(definition["medios"]!.AsArray().Select(x => KeyValuePair.Create<string, JsonNode?>(x!.GetValue<string>(), JsonValue.Create(false)))),
            ["preguntas"] = new JsonArray(definition["preguntas"]!.AsArray().Select(q => (JsonNode?)new JsonObject
            {
                ["pregunta"] = q!["texto"]!.DeepClone(),
                ["tipo"] = q["tipo"]!.DeepClone(),
                ["opciones"] = q["opciones"]?.DeepClone(),
                ["marcada"] = false,
                ["respuesta"] = ""
            }).ToArray())
        };
        foreach (var (section, fields) in Fields)
        {
            var row = new JsonObject(fields.Select(f => KeyValuePair.Create<string, JsonNode?>(f, JsonValue.Create(f == "id" ? "inicial" : f == "fuente" && section == "identificacion" ? "Nuevo" : ""))));
            if (section == "identificacion") row["usuario"] = new JsonArray();
            result[section] = new JsonArray(row);
        }
        return result;
    }
    private static DomainProblem Invalid(string message = "El contenido del formato no es válido.") => new(422, message, new { errors = new { contenido = new[] { message } } });
    private static JsonObject Object(JsonNode? node, params string[] keys)
    {
        if (node is not JsonObject value || keys.Length > 0 && value.Any(p => !keys.Contains(p.Key))) throw Invalid();
        return value;
    }
    private static JsonArray Array(JsonNode? node, int max = 200)
    {
        if (node is not JsonArray array || array.Count > max) throw Invalid();
        return array;
    }
    private static string Text(JsonObject node, string key, int max = 4000)
    {
        if (!node.ContainsKey(key)) throw Invalid();
        if (node[key] is null) return "";
        if (node[key] is not JsonValue value || !value.TryGetValue<string>(out var text) || text.EnumerateRunes().Count() > max) throw Invalid();
        return text;
    }
    private static bool Boolean(JsonNode? node) => node is JsonValue value && value.TryGetValue<bool>(out var boolean) ? boolean : throw Invalid();
    private static void Choice(string text, params string[] choices) { if (text != "" && !choices.Contains(text)) throw Invalid(); }
    private static void Date(string text)
    {
        if (text != "" && !DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)) throw Invalid();
    }
    public JsonObject Validate(JsonObject input, Unit unit)
    {
        if (Encoding.UTF8.GetByteCount(input.ToJsonString()) > 1_500_000) throw Invalid("El formato supera el tamaño permitido.");
        var result = Blank(unit);
        // Datos de sesión retirados: conservar su historia sin normalizarla ni exigir campos invisibles.
        if (input.ContainsKey("encabezado")) result["encabezado"] = input["encabezado"]?.DeepClone();
        result["medioOtro"] = Text(input, "medioOtro", 500);
        foreach (var (section, fields) in Fields)
        {
            var rows = Array(input[section]); var ids = new HashSet<string>(StringComparer.Ordinal);
            var normalized = new JsonArray();
            foreach (var item in rows)
            {
                var row = Object(item, fields); var clean = new JsonObject();
                foreach (var field in fields) clean[field] = section == "identificacion" && field == "usuario"
                    ? Users(row[field]) : JsonValue.Create(Text(row, field, field == "id" ? 64 : 4000));
                var id = Text(row, "id", 64);
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw Invalid("Las filas necesitan identificadores únicos.");
                normalized.Add(clean);
            }
            result[section] = normalized;
        }
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in result["identificacion"]!.AsArray().Cast<JsonObject>())
        {
            var code = Text(row, "codigo"); var priority = Text(row, "prioridad");
            if (code != "" && (!Regex.IsMatch(code, @"\APO-[0-9]{2,6}\z") || !codes.Add(code))
                || priority != "" && !Regex.IsMatch(priority, @"\A[1-9][0-9]{0,3}\z")) throw Invalid("Código o prioridad inválidos.");
            Choice(Text(row, "validacion"), "V", "A", "D", "N");
        }
        foreach (var section in new[] { "sistemas", "datos" })
            foreach (var row in result[section]!.AsArray().Cast<JsonObject>())
            {
                if (Text(row, "proceso") is { Length: > 0 } code && !codes.Contains(code)) throw Invalid("El proceso seleccionado no pertenece al formato.");
                if (section == "sistemas") Choice(Text(row, "estado"), "Funciona", "Parcial", "No funciona", "No se usa");
                else Choice(Text(row, "origen"), "Se origina en este proceso", "Se origina en otro proceso", "Se genera en otra instancia");
            }
        foreach (var row in result["acuerdos"]!.AsArray().Cast<JsonObject>()) Date(Text(row, "fecha"));
        var media = Object(input["medios"]);
        foreach (var (_, value) in media) Boolean(value);
        foreach (var (key, _) in result["medios"]!.AsObject().ToArray()) result["medios"]![key] = media.TryGetPropertyValue(key, out var value) && Boolean(value);
        // Laravel serializa un arreglo asociativo vacío como []; sólo se acepta esa forma heredada si está vacía.
        var evaluations = input["evaluaciones"] is JsonArray { Count: 0 } ? new JsonObject() : Object(input["evaluaciones"]);
        if (evaluations.Count > 200) throw Invalid();
        foreach (var (code, entries) in evaluations)
        {
            var rows = Array(entries, 9);
            if (!codes.Contains(code) || rows.Count != 9) throw Invalid("La evaluación no corresponde a un proceso del formato.");
            var normalized = new JsonArray();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = Object(rows[i], "criterio", "valor", "obs");
                if (string.IsNullOrWhiteSpace(Text(row, "criterio", 100))) throw Invalid();
                var value = Text(row, "valor"); Choice(value, "1", "2", "3", "4", "5");
                normalized.Add(new JsonObject { ["criterio"] = definition["criterios"]![i]!.DeepClone(), ["valor"] = value, ["obs"] = Text(row, "obs") });
            }
            result["evaluaciones"]![code] = normalized;
        }
        var questions = Array(input["preguntas"], 12);
        if (questions.Count != 12) throw Invalid("El formato debe conservar las doce preguntas generales.");
        for (var i = 0; i < questions.Count; i++)
        {
            var row = Object(questions[i], "pregunta", "tipo", "opciones", "marcada", "respuesta");
            var answer = Text(row, "respuesta", 8000);
            if (definition["preguntas"]![i]!["opciones"] is JsonArray options) Choice(answer, options.Select(x => x!.GetValue<string>()).ToArray());
            result["preguntas"]![i]!["respuesta"] = answer;
            result["preguntas"]![i]!["marcada"] = Boolean(row["marcada"]);
        }
        return result;
    }
    /// <summary>Calcula el avance canónico con los pesos de la plantilla; se persiste dentro del guardado.</summary>
    /// <remarks>No aceptar el porcentaje del navegador ni recalcular respuestas durante una sincronización.</remarks>
    public static int Progress(JsonObject data)
    {
        static bool Filled(JsonNode? node) => !string.IsNullOrWhiteSpace(node?.GetValue<string>());
        static double Score(IEnumerable<JsonNode?> nodes, string[] fields, int weight)
        {
            var rows = nodes.ToArray();
            return (double)weight * rows.Sum(r => fields.Count(k => k == "usuario" ? HasUsers(r?[k]) : Filled(r?[k]))) / (Math.Max(1, rows.Length) * fields.Length);
        }
        var value = Score(data["identificacion"]!.AsArray(), ["tramite", "usuario", "resultado", "responsable", "validacion", "prioridad"], 25)
            + Score(data["sistemas"]!.AsArray(), ["sistema", "uso", "estado"], 15)
            + Score(data["datos"]!.AsArray(), ["dato", "fuente", "origen"], 10)
            + Score(data["preguntas"]!.AsArray(), ["respuesta"], 20)
            + Score(data["acuerdos"]!.AsArray(), ["acuerdo", "responsable", "fecha"], 5);
        var codes = data["identificacion"]!.AsArray().Where(r => Filled(r!["codigo"])).Select(r => r!["codigo"]!.GetValue<string>()).ToArray();
        var filled = codes.Sum(c => ((data["evaluaciones"] as JsonObject)?[c] as JsonArray)?.Count(r => Filled(r?["valor"])) ?? 0);
        // Se redistribuyen proporcionalmente los 90 puntos vigentes; no se inventan respuestas de sesión.
        return (int)Math.Floor((value + 15d * filled / (Math.Max(1, codes.Length) * 9)) * 100 / 90 + 0.000001);
    }

    public static void RequireComplete(JsonObject content)
    {
        var review = FormReview.Inspect(content);
        if (!review.listo) throw new DomainProblem(422, "Completa los pendientes de Revisión y envío antes de enviar.", new { revisionEnvio = review });
    }
}
