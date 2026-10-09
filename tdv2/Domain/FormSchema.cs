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
    public static readonly string[] UserChoices = ["Comunidad universitaria", "Docentes", "Estudiantes", "Personal administrativo",
        "Público en general", "Instituciones públicas externas", "Empresas y organizaciones privadas"];
    public static bool HasRecipients(JsonNode? row) => HasUsers(row?["usuario"]);
    public static bool HasUsers(JsonNode? node) => node is JsonArray { Count: > 0 } users
        && users.All(v => v is JsonValue value && value.TryGetValue<string>(out var text) && UserChoices.Contains(text, StringComparer.Ordinal))
        && users.Select(v => v!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count() == users.Count;
    private static JsonArray Users(JsonNode? node)
    {
        if (node is not JsonArray users || users.Count > UserChoices.Length || users.Count > 0 && !HasUsers(users))
            throw Invalid("Selecciona opciones válidas en ¿A quién atiende?, sin repeticiones.");
        return users.DeepClone().AsArray();
    }
    private static readonly Dictionary<string, string[]> Fields = new()
    {
        ["identificacion"] = ["id", "codigo", "prioridad", "fuente", "area", "tramite", "usuario", "resultado", "responsable", "validacion"],
        ["sistemas"] = ["id", "proceso", "sistema", "uso", "estado", "fallas", "sistemaOtro", "usoOtro", "moduloSiiId", "moduloSiiDescripcion"],
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
    public static DomainProblem FieldError(string block, string field, string message) => new(422, message,
        new { validation = new[] { new { block, field, message } } });
    private static T At<T>(string block, string field, Func<T> action)
    {
        try { return action(); }
        catch (DomainProblem error) when (error.Status == 422) { throw FieldError(block, field, error.Message); }
    }
    public JsonObject Validate(JsonObject input, Unit unit, IReadOnlySet<string>? changed = null, IReadOnlySet<string>? removedCodes = null, JsonObject? previous = null)
    {
        bool Includes(string key) => changed is null || changed.Contains(key);
        void Check(string block, string field, Action action) => At(block, field, () => { action(); return true; });
        if (Encoding.UTF8.GetByteCount(input.ToJsonString()) > 1_500_000) throw Invalid("El formato supera el tamaño permitido.");
        var result = Blank(unit);
        // Datos de sesión retirados: conservar su historia sin normalizarla ni exigir campos invisibles.
        if (input.ContainsKey("encabezado")) result["encabezado"] = input["encabezado"]?.DeepClone();
        result["medioOtro"] = Includes("medios") ? JsonValue.Create(At("medios", "medioOtro", () => Text(input, "medioOtro", 500))) : input["medioOtro"]?.DeepClone();
        foreach (var (section, fields) in Fields)
        {
            if (changed is not null && !changed.Any(k => k.StartsWith(section + ":", StringComparison.Ordinal)))
            { result[section] = input[section]?.DeepClone(); continue; }
            var rows = Array(input[section]); var ids = new HashSet<string>(StringComparer.Ordinal);
            var normalized = new JsonArray();
            foreach (var item in rows)
            {
                var row = Object(item); var clean = new JsonObject();
                var id = Text(row, "id", 64); var key = section + ":" + id;
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) throw FieldError(key, "id", "Las filas necesitan identificadores únicos.");
                // El parche no normaliza respuestas históricas de otros registros. El envío valida los bloques de la entrega vigente.
                if (!Includes(key)) { normalized.Add(row.DeepClone()); continue; }
                // Una petición antigua no puede reintroducir el detalle retirado tras la migración.
                if (section == "identificacion" && row.ContainsKey("usuarioOtro"))
                    throw FieldError(key, "usuario", "El catálogo de ¿A quién atiende? cambió. Actualiza el formato antes de guardar.");
                Object(row, fields);
                foreach (var field in fields) clean[field] = At<JsonNode?>(key, field, () => section == "identificacion" && field == "usuario"
                    ? Users(row[field]) : section == "sistemas" && SystemAnswers.Details.Contains(field) && !row.ContainsKey(field)
                    ? JsonValue.Create("") : JsonValue.Create(Text(row, field, field == "id" ? 64 : 4000)));
                normalized.Add(clean);
            }
            result[section] = normalized;
        }
        var codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in result["identificacion"]!.AsArray().Cast<JsonObject>())
        {
            var key = "identificacion:" + row["id"]; var code = Text(row, "codigo");
            if (code != "" && (!Regex.IsMatch(code, @"\APO-[0-9]{2,6}\z") || !codes.Add(code)))
                throw FieldError(key, "codigo", "Código de proceso inválido o repetido.");
            if (!Includes(key)) continue;
            var priority = Text(row, "prioridad");
            if (priority != "" && !Regex.IsMatch(priority, @"\A[1-9][0-9]{0,3}\z")) throw FieldError(key, "prioridad", "Prioridad inválida.");
            Check(key, "validacion", () => Choice(Text(row, "validacion"), "V", "A", "D", "N"));
        }
        foreach (var section in new[] { "sistemas", "datos" }.Where(s => changed is null || changed.Any(k => k.StartsWith(s + ":"))))
            foreach (var row in result[section]!.AsArray().Cast<JsonObject>())
            {
                var key = section + ":" + row["id"]; var code = Text(row, "proceso");
                // Retirar un proceso obliga a validar también sus vínculos, aunque no vengan en el parche.
                var unchangedLink = previous?[section]?.AsArray().Any(r => r?["id"]?.ToString() == row["id"]?.ToString() && r?["proceso"]?.ToString() == code) == true;
                if (code.Length > 0 && !codes.Contains(code) && (Includes(key) && !unchangedLink || removedCodes?.Contains(code) == true))
                    throw FieldError(key, "proceso", "El proceso seleccionado no pertenece al formato.");
                if (section == "sistemas" && code.Length > 0 && Includes(key) && !unchangedLink
                    && !ProcedureEligibility.Contains(result, code))
                    throw FieldError(key, "proceso", "Selecciona un procedimiento completo y válido del formato, marcado como Vigente o Ajustar.");
                if (!Includes(key)) continue;
                if (section == "sistemas") SystemAnswers.Validate(row, previous?[section]?.AsArray().FirstOrDefault(r => r?["id"]?.ToString() == row["id"]?.ToString()), key);
                else Check(key, "origen", () => Choice(Text(row, "origen"), "Se origina en este proceso", "Se origina en otro proceso", "Se genera en otra instancia"));
            }
        foreach (var row in result["acuerdos"]!.AsArray().Cast<JsonObject>())
            if (Includes("acuerdos:" + row["id"])) Check("acuerdos:" + row["id"], "fecha", () => Date(Text(row, "fecha")));
        var media = Object(input["medios"]);
        foreach (var (_, value) in media) Boolean(value);
        foreach (var (key, _) in result["medios"]!.AsObject().ToArray()) result["medios"]![key] = media.TryGetPropertyValue(key, out var value) && Boolean(value);
        // Laravel serializa un arreglo asociativo vacío como []; sólo se acepta esa forma heredada si está vacía.
        if (changed is not null && !changed.Any(k => k.StartsWith("evaluaciones:", StringComparison.Ordinal)))
            result["evaluaciones"] = input["evaluaciones"]?.DeepClone();
        else
        {
            var evaluations = input["evaluaciones"] is JsonArray { Count: 0 } ? new JsonObject() : Object(input["evaluaciones"]);
            if (evaluations.Count > 200) throw Invalid();
            foreach (var (code, entries) in evaluations)
            {
                var rows = Array(entries, 9);
                if (rows.Count != 9 || !codes.Contains(code) && (changed is null || removedCodes?.Contains(code) == true || changed.Any(k => k.StartsWith($"evaluaciones:{code}:"))))
                    throw FieldError($"evaluaciones:{code}:0", "criterio", "La evaluación no corresponde a un proceso del formato.");
                var normalized = new JsonArray();
                for (var i = 0; i < rows.Count; i++)
                {
                    var key = $"evaluaciones:{code}:{i}";
                    if (!Includes(key)) { normalized.Add(rows[i]?.DeepClone()); continue; }
                    var row = Object(rows[i], "criterio", "valor", "obs");
                    if (string.IsNullOrWhiteSpace(Text(row, "criterio", 100))) throw Invalid();
                    var value = Text(row, "valor"); Check(key, "valor", () => Choice(value, "1", "2", "3", "4", "5"));
                    normalized.Add(new JsonObject { ["criterio"] = definition["criterios"]![i]!.DeepClone(), ["valor"] = value, ["obs"] = At(key, "obs", () => Text(row, "obs")) });
                }
                result["evaluaciones"]![code] = normalized;
            }
        }
        if (changed is not null && !changed.Any(k => k.StartsWith("preguntas:", StringComparison.Ordinal)))
            result["preguntas"] = input["preguntas"]?.DeepClone();
        else
        {
            var questions = Array(input["preguntas"], 12);
            if (questions.Count != 12) throw Invalid("El formato debe conservar las doce preguntas generales.");
            for (var i = 0; i < questions.Count; i++)
            {
                var key = "preguntas:" + i;
                if (!Includes(key)) { result["preguntas"]![i] = questions[i]?.DeepClone(); continue; }
                var row = Object(questions[i], "pregunta", "tipo", "opciones", "marcada", "respuesta");
                var answer = At(key, "respuesta", () => Text(row, "respuesta", 8000));
                if (definition["preguntas"]![i]!["opciones"] is JsonArray options) Check(key, "respuesta", () => Choice(answer, options.Select(x => x!.GetValue<string>()).ToArray()));
                result["preguntas"]![i]!["respuesta"] = answer;
                result["preguntas"]![i]!["marcada"] = Boolean(row["marcada"]);
            }
        }
        return result;
    }
    /// <summary>Genera códigos bajo el bloqueo de UR; los selectores sólo ofrecen códigos ya confirmados.</summary>
    public string[] CompleteProcesses(JsonObject content, IReadOnlySet<string> changed, IEnumerable<string> historicalKeys)
    {
        var codes = content["identificacion"]!.AsArray().Select(r => r!["codigo"]?.ToString() ?? "")
            .Concat(historicalKeys.Where(k => k.StartsWith("evaluaciones:")).Select(k => k.Split(':')[1]));
        var next = codes.Where(c => Regex.IsMatch(c, @"\APO-[0-9]{2,6}\z")).Select(c => int.Parse(c[3..], CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max();
        if (content["evaluaciones"] is not JsonObject) content["evaluaciones"] = new JsonObject();
        var generated = new List<string>();
        foreach (var row in content["identificacion"]!.AsArray().Cast<JsonObject>())
        {
            var key = "identificacion:" + row["id"];
            if (!changed.Contains(key)) continue;
            if (row["codigo"]?.ToString() == "" && row["validacion"]?.ToString() is { Length: > 0 })
            {
                if (next >= 999999) throw FieldError(key, "validacion", "Se alcanzó el límite de códigos del formato.");
                row["codigo"] = "PO-" + (++next).ToString("D2", CultureInfo.InvariantCulture);
            }
            if (row["codigo"]?.ToString() is not { Length: > 0 } code || content["evaluaciones"]![code] is not null) continue;
            // Inicializa criterios ausentes, nunca respuestas ni evaluaciones existentes.
            content["evaluaciones"]![code] = new JsonArray(definition["criterios"]!.AsArray().Select(c => (JsonNode?)new JsonObject
                { ["criterio"] = c!.DeepClone(), ["valor"] = "", ["obs"] = "" }).ToArray());
            generated.AddRange(Enumerable.Range(0, 9).Select(i => $"evaluaciones:{code}:{i}"));
        }
        return generated.ToArray();
    }
    public static int Progress(JsonObject data) => FormCapture.Progress(data);

    public static void RequireComplete(JsonObject content)
    {
        var review = FormReview.Inspect(content);
        if (!review.listo) throw new DomainProblem(422, "Completa los pendientes de Revisión y envío antes de enviar.", new { revisionEnvio = review });
    }
}
