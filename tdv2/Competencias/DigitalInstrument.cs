using System.Text.Json;
using System.Text.Json.Nodes;
using Tdv2.Domain;

namespace Tdv2.Competencias;

public sealed record DigitalAnswer(string State, int? Level = null, string? Checks = null);
public sealed record DigitalTool(string Id, string N, string S, string Cat, string[][] Lv, string[] C, string? Lic, bool Doc, bool Opt);
public sealed record DigitalBlock(string Id, string N, string D, DigitalTool[] Tools, string[] Trans);
public sealed record DigitalToolResult(string Id, string Name, string Block, int? Value, int Expected, int Gap);
public sealed record DigitalBlockResult(string Id, string Name, double? Score, int? Level);
public sealed record DigitalCourse(string Tool, string Name, string Block, int Phase, int From, int To, string Why, string[] Courses, string[] Missing, string? License);
public sealed record DigitalRecommendation(string Kind, string Title, string Text);
public sealed record DigitalReport(double? Global, int? Level, DigitalBlockResult[] Blocks, DigitalToolResult[] Tools,
    DigitalCourse[] Route, string[] Transversal, DigitalRecommendation[] Recommendations);
public sealed record DigitalEvaluation(int Answered, int Total, double Progress, string[] Pending, DigitalReport Report);
public sealed record DigitalStep(string Id, string Name, bool Complete, bool Accessible, string[] Pending);
public sealed record DigitalNavigation(DigitalStep[] Steps, string Resume, string Current);

public sealed class DigitalInstrument
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public JsonObject Resource { get; }
    public string Version => Resource["version"]!.GetValue<string>();
    public string NoticeVersion => Resource["notice"]!["version"]!.GetValue<string>();
    public DigitalBlock[] Blocks { get; }
    public DigitalInstrument(string content)
    {
        Resource = JsonNode.Parse(content)!.AsObject();
        Blocks = Resource["modules"]!.Deserialize<DigitalBlock[]>(Json)!;
        if (Blocks.Length != 5 || Blocks.SelectMany(b => b.Tools).Select(t => t.Id).Distinct().Count() != (Resource["toolCount"]?.GetValue<int>() ?? 84))
            throw new InvalidOperationException("Instrumento incompleto.");
    }
    public string? RulePosition(string? position) => position is null ? null : Resource["positionRules"] is JsonObject rules
        ? rules[position]?.GetValue<string>() ?? throw new DomainProblem(422, "Selecciona un perfil laboral vigente.") : position;
    public bool Academic(JsonObject profile) => RulePosition(profile["puesto"]?.ToString()) == "Personal docente";
    public static int ChecklistLevel(string checks)
    {
        if (checks.Length != 12 || checks.Any(c => c is not ('0' or '1'))) throw new DomainProblem(422, "La rúbrica debe contener sus doce indicadores.");
        var level = 0;
        for (var i = 0; i < 3; i++) { if (checks.Skip(i * 4).Take(4).Count(c => c == '1') < 3) break; level++; }
        return level;
    }
    public static int? ScoreLevel(double? score) => score is null ? null : score < 25 ? 0 : score < 50 ? 1 : score < 75 ? 2 : 3;
    public static double? Mean(IEnumerable<double?> values)
    {
        var data = values.Where(v => v is not null).Select(v => v!.Value).ToArray();
        return data.Length == 0 ? null : data.Average();
    }
    // La secuencia se deriva de la misma evaluación que el informe, nunca de marcas del cliente.
    public DigitalNavigation Navigation(DigitalEvaluation evaluation, bool institutionAvailable, string? requested = null)
    {
        var profilePending = evaluation.Pending.Where(p => p.StartsWith("perfil:", StringComparison.Ordinal)).ToList();
        if (!institutionAvailable) profilePending.Add("perfil:institucion");
        var steps = new List<DigitalStep> { new("perfil", "Perfil", profilePending.Count == 0, true, profilePending.ToArray()) };
        foreach (var block in Blocks)
        {
            var pending = evaluation.Pending.Where(p => block.Tools.Any(t => t.Id == p)).ToArray();
            steps.Add(new(block.Id, block.N, pending.Length == 0, steps.All(s => s.Complete), pending));
        }
        var resume = steps.FirstOrDefault(s => !s.Complete)?.Id ?? steps[^1].Id;
        // URL desconocida o bloqueada: recuperar el primer pendiente persistido, sin saltos.
        var current = steps.Any(s => s.Id == requested && s.Accessible) ? requested! : resume;
        return new(steps.ToArray(), resume, current);
    }
    public void ValidateSectionChanges(JsonObject previousProfile, IReadOnlyDictionary<string, DigitalAnswer> previousAnswers,
        JsonObject profile, IReadOnlyDictionary<string, DigitalAnswer> answers, bool institutionAvailable)
    {
        var before = Navigation(Evaluate(previousProfile, previousAnswers), institutionAvailable);
        var after = Navigation(Evaluate(profile, answers), institutionAvailable);
        foreach (var block in Blocks)
        {
            // Un documento completo puede conservar respuestas posteriores; solo se comprueban modificaciones.
            var changed = block.Tools.Any(t => previousAnswers.GetValueOrDefault(t.Id) != answers.GetValueOrDefault(t.Id));
            if (changed && (!before.Steps.Single(s => s.Id == block.Id).Accessible || !after.Steps.Single(s => s.Id == block.Id).Accessible))
                throw new DomainProblem(422, "Completa y guarda las secciones anteriores antes de modificar este bloque.",
                    new { pending = (before.Steps.FirstOrDefault(s => !s.Complete) ?? after.Steps.First(s => !s.Complete)).Pending, step = after.Resume });
        }
    }
    public Dictionary<string, int> Requirements(string? position)
    {
        position = RulePosition(position);
        var result = new Dictionary<string, int>();
        foreach (var f in Resource["funcs"]!.AsArray())
        {
            var cap = f!["p"]!.ToString() == position ? 3 : f["p"]!.ToString() == "Personal operativo" && f["m"]!.ToString() != "sii" ? 1 : 0;
            if (cap == 0) continue;
            foreach (var pair in f["req"]!.AsArray())
            {
                var key = pair![0]!.ToString(); var value = Math.Min(cap, pair[1]!.GetValue<int>());
                result[key] = Math.Max(result.GetValueOrDefault(key), value);
            }
        }
        return result;
    }
    public void ValidateProfile(JsonObject profile)
    {
        var fields = Resource["profile"]!.AsArray();
        if (profile.Any(p => !fields.Any(f => f!["id"]!.ToString() == p.Key))) throw new DomainProblem(422, "El perfil contiene campos no permitidos.");
        foreach (var field in fields)
        {
            var id = field!["id"]!.ToString(); var value = profile[id];
            if (value is null) continue;
            var choices = field["options"]!.AsArray().Select(x => x!.ToString()).ToArray();
            if (id == "funciones")
            {
                if (value is not JsonArray a || a.Count > choices.Length || a.Any(x => x is not JsonValue || !choices.Contains(x.ToString())) || a.Select(x => x!.ToString()).Distinct().Count() != a.Count)
                    throw new DomainProblem(422, "Selecciona funciones válidas.");
            }
            else if (value is not JsonValue || !choices.Contains(value.ToString())) throw new DomainProblem(422, "Selecciona una opción válida del perfil.");
        }
    }
    public DigitalEvaluation Evaluate(JsonObject profile, IReadOnlyDictionary<string, DigitalAnswer> answers)
    {
        ValidateProfile(profile);
        var known = Blocks.SelectMany(b => b.Tools).ToDictionary(t => t.Id);
        if (answers.Keys.Any(k => !known.ContainsKey(k))) throw new DomainProblem(422, "La respuesta no pertenece al instrumento.");
        var position = profile["puesto"]?.ToString(); var requirements = Requirements(position);
        var pending = new List<string>();
        foreach (var field in Resource["profile"]!.AsArray())
        {
            var id = field!["id"]!.ToString();
            // Se conservan todas las preguntas del perfil; ahora todas deben completarse.
            if (profile[id] is null || profile[id] is JsonArray a && a.Count == 0) pending.Add("perfil:" + id);
        }
        var tools = new List<DigitalToolResult>(); var blocks = new List<DigitalBlockResult>();
        var total = Resource["profile"]!.AsArray().Count;
        var answered = total - pending.Count;
        foreach (var block in Blocks)
        {
            var blockValues = new List<double?>();
            foreach (var tool in block.Tools)
            {
                if (tool.Doc && !Academic(profile)) continue;
                var answer = answers.GetValueOrDefault(tool.Id); int? value = null;
                // Las complementarias no seleccionadas son «No utiliza», no un cero ni un pendiente.
                if (answer is null && tool.Opt) value = -2;
                else if (answer is not null)
                {
                    if (answer.State == "pending" && answer.Level is null && answer.Checks is null) value = null;
                    else if (answer.State == "unused" && tool.Opt && answer.Level is null && answer.Checks is null) value = -2;
                    else if (answer.State == "na" && answer.Level is null && answer.Checks is null) value = -1;
                    else if (answer.State == "level")
                    {
                        if (block.Id is "ciber" or "datos" or "sii")
                        {
                            if (answer.Checks is null || answer.Level is not null) throw new DomainProblem(422, "El nivel de esta rúbrica lo calcula el servidor.");
                            value = ChecklistLevel(answer.Checks);
                        }
                        else if (answer.Level is >= 0 and <= 3 && answer.Checks is null) value = answer.Level;
                        else throw new DomainProblem(422, "Selecciona un nivel válido.");
                    }
                    else throw new DomainProblem(422, "El estado de la respuesta no es válido.");
                }
                if (value != -2) { total++; if (value is not null) answered++; else pending.Add(tool.Id); }
                if (value >= 0) blockValues.Add(value.Value / 3d * 100);
                var expected = requirements.GetValueOrDefault(tool.Id);
                tools.Add(new(tool.Id, tool.N, block.Id, value, expected, value >= 0 ? Math.Max(0, expected - value.Value) : value == -2 ? expected : 0));
            }
            var score = Mean(blockValues); blocks.Add(new(block.Id, block.N, score, ScoreLevel(score)));
        }
        return new(answered, total, total == 0 ? 0 : answered * 100d / total, pending.ToArray(), BuildReport(profile, answers, tools, blocks, known));
    }
    // Sólo la proyección preliminar distingue ausencia de selección de «No utiliza» explícito.
    // No modifica validación, avance, evaluación definitiva ni informes históricos.
    public static bool HasResponses(DigitalReport report, IReadOnlyDictionary<string, DigitalAnswer> answers) =>
        report.Tools.Any(t => t.Value is not null && answers.TryGetValue(t.Id, out var a) && a.State != "pending");
    public DigitalReport PreliminaryReport(JsonObject profile, IReadOnlyDictionary<string, DigitalAnswer> answers, DigitalReport evaluated)
    {
        var tools = evaluated.Tools.Select(t => !answers.ContainsKey(t.Id) ? t with { Value = null, Gap = 0 } : t).ToList();
        return BuildReport(profile, answers, tools, evaluated.Blocks.ToList(), Blocks.SelectMany(b => b.Tools).ToDictionary(t => t.Id));
    }
    private DigitalReport BuildReport(JsonObject profile, IReadOnlyDictionary<string, DigitalAnswer> answers, List<DigitalToolResult> tools,
        List<DigitalBlockResult> blocks, Dictionary<string, DigitalTool> known)
    {
        var position = RulePosition(profile["puesto"]?.ToString());
        var global = Mean(blocks.Select(b => b.Score));
        var route = Route(profile, answers, tools, known);
        var transversal = Resource["funcs"]!.AsArray().Where(f => f!["p"]!.ToString() == position && f["t"] is not null
            && f["req"]!.AsArray().Any(pair => tools.Any(t => t.Id == pair![0]!.ToString() && (t.Value >= 0 || t.Value == -2) && t.Value < pair[1]!.GetValue<int>())))
            .SelectMany(f => f!["t"]!.ToString().Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).Distinct().ToArray();
        return new(global, ScoreLevel(global), blocks.ToArray(), tools.ToArray(), route, transversal, PersonalRecommendations(profile, tools, blocks, transversal));
    }
    private DigitalCourse[] Route(JsonObject profile, IReadOnlyDictionary<string, DigitalAnswer> answers, List<DigitalToolResult> results, Dictionary<string, DigitalTool> known)
    {
        var route = new List<DigitalCourse>();
        void Add(DigitalToolResult r, int phase, int from, int to, string reason)
        {
            var t = known[r.Id];
            var missing = answers.GetValueOrDefault(t.Id)?.Checks is { } checks ? t.Lv[from].Where((_, i) => checks[from * 4 + i] != '1').ToArray() : [];
            route.Add(new(t.Id, t.N, r.Block, phase, from, to, reason, t.C.Skip(from).Take(to - from).ToArray(), missing, t.Lic));
        }
        foreach (var r in results.Where(t => t.Value >= 0 || t.Value == -2 && t.Expected > 0))
        {
            var v = Math.Max(0, r.Value!.Value); var t = known[r.Id];
            if (v < r.Expected) Add(r, 1, v, r.Expected, "Tu puesto requiere " + Resource["levels"]![r.Expected]!.ToString());
            else if (v < 1 && (r.Block == "ciber" || r.Block == "ia" && t.Cat == "Fundamentos del uso de IA" || r.Block == "datos" && profile["datosP"]?.ToString() == "Sí" && (t.N.Contains("personales") || t.N.Contains("Clasificación"))))
                Add(r, 1, v, 1, r.Block == "ciber" ? "Riesgo de seguridad" : r.Block == "ia" ? "Base para usar IA con seguridad" : "Manejas datos personales");
            else if (v is 1 or 2) Add(r, 2, v, v + 1, v == 1 ? "Ya la usas: consolida el nivel intermedio" : "Da el paso a especialista");
            else if (v == 0 && r.Block != "ia") Add(r, 3, 0, 1, "Explorar");
        }
        var picks = Academic(profile)
            ? new[] { "i-chatgpt", "i-notebooklm", "i-teachy", "i-gamma", "i-quizizz-wayground" }
            : ["i-microsoft-copilot", "i-perplexity", "i-notebooklm", "i-napkin-ai", "i-fireflies"];
        foreach (var r in results.Where(r => picks.Contains(r.Id) && r.Value is 0 or -2 && !route.Any(c => c.Tool == r.Id))) Add(r, 3, 0, 1, "Sugerida para tu perfil en el Manual de IA");
        var order = new Dictionary<string, int> { ["ciber"] = 0, ["datos"] = 1, ["sii"] = 2, ["ia"] = 3, ["m365"] = 4 };
        return route.OrderBy(c => c.Phase).ThenByDescending(c => c.Phase == 1 ? c.To - c.From : c.Phase == 2 && results.Any(r => r.Id == c.Tool && r.Expected > 0) ? 1 : 0)
            .ThenBy(c => c.Phase == 2 ? c.From : 0).ThenBy(c => order[c.Block]).ToArray();
    }
    private DigitalRecommendation[] PersonalRecommendations(JsonObject profile, List<DigitalToolResult> tools, List<DigitalBlockResult> blocks, string[] transversal)
    {
        var recs = new List<DigitalRecommendation>();
        var gaps = tools.Where(t => t.Gap > 0).OrderByDescending(t => t.Gap).ToArray();
        if (gaps.Length > 0) recs.Add(new("warning", $"Tienes {gaps.Length} brechas frente a las funciones de tu puesto", "Las más importantes: " + string.Join(", ", gaps.Take(4).Select(t => t.Name)) + ". Empieza por la Fase 1 de tu ruta."));
        else if (tools.Any(t => t.Value >= 0)) recs.Add(new("success", "Sin brechas en las herramientas evaluadas frente a tu puesto", "Usa la Fase 2 para especializarte. Las respuestas No aplica y No utiliza no acreditan competencia."));
        var cyber = tools.Where(t => t.Block == "ciber" && t.Value == 0).ToArray();
        if (cyber.Length > 0) recs.Add(new("warning", "Refuerza tu seguridad digital esta semana", string.Join(", ", cyber.Select(t => t.Name)) + ". Activa la autenticación multifactor, usa contraseñas largas y distintas, y reporta correos sospechosos con el botón Informar de Outlook."));
        if (profile["datosP"]?.ToString() == "Sí" && tools.Any(t => t.Name == "Tratamiento de datos personales" && t.Value >= 0 && t.Value < 2))
            recs.Add(new("warning", "Manejas datos personales: sube a nivel Intermedio en su tratamiento", "Revisa los avisos de privacidad, recaba solo los datos necesarios y anonimiza antes de compartir o analizar. Canaliza las solicitudes ARCO a la Unidad de Transparencia."));
        var ia = Blocks.Single(b => b.Id == "ia");
        var used = tools.Where(t => t.Block == "ia" && t.Value >= 1 && ia.Tools.Single(i => i.Id == t.Id).Cat != "Fundamentos del uso de IA").ToArray();
        var low = tools.Where(t => t.Block == "ia" && t.Value == 0 && ia.Tools.Single(i => i.Id == t.Id).Cat == "Fundamentos del uso de IA").ToArray();
        if (used.Length > 0 && low.Length > 0) recs.Add(new("warning", "Usas IA, pero sin los fundamentos completos", string.Join(", ", low.Select(t => t.Name)) + ". La IA es un copiloto, no un piloto automático. Verifica siempre y no subas datos de estudiantes a herramientas públicas."));
        if (used.Length == 0) recs.Add(new("info", "Empieza pequeño con la IA", "Elige una herramienta y una tarea concreta. " + (Academic(profile) ? "ChatGPT o Teachy para planear una clase" : "Copilot Chat con tu cuenta institucional para resumir y redactar documentos") + ", aplicando ROTF (Rol, Objetivo, Contexto, Formato)."));
        if (used.Any(t => new[] { "i-chatgpt", "i-claude", "i-gemini", "i-deepseek" }.Contains(t.Id)) && tools.Any(t => t.Id == "m-microsoft-365-copilot-chat" && t.Value == 0))
            recs.Add(new("info", "Para información institucional, consulta las herramientas autorizadas", "El prototipo recomienda Copilot Chat con la cuenta institucional. Confirma las condiciones y licencias vigentes antes de compartir información."));
        var weakest = blocks.Where(b => b.Score is not null).OrderBy(b => b.Score).FirstOrDefault();
        if (weakest is not null) recs.Add(new("info", "Tu bloque con más oportunidad: " + weakest.Name, "Dedica a este bloque al menos un curso por mes durante la Fase 2."));
        var strong = tools.Where(t => t.Value == 3).ToArray();
        if (strong.Length > 0) recs.Add(new("success", "Comparte lo que dominas", string.Join(", ", strong.Take(6).Select(t => t.Name)) + ". Puedes apoyar como mentora o mentor y proponer mejoras a los procesos del área."));
        if (transversal.Length > 0) recs.Add(new("info", "Cursos transversales recomendados para tu puesto", string.Join(" · ", transversal)));
        return recs.ToArray();
    }
}
