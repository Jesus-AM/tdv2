using System.Text.Json.Nodes;
namespace Tdv2.Domain;

public sealed record FormPending(string seccion, string bloque, string campo, string mensaje);
public sealed record FormReview(bool listo, IReadOnlyList<FormPending> pendientes)
{
    /// <summary>La misma revisión alimenta los enlaces de captura y la autorización final del envío.</summary>
    public static FormReview Inspect(JsonObject content, bool firstStage = true)
    {
        var pending = new List<FormPending>();
        void Rows(string section, string title, params (string Key, string Label)[] fields)
        {
            var rows = content[section]!.AsArray();
            if (rows.Count == 0) pending.Add(new(section, section, "", $"{title}: agrega al menos un registro."));
            foreach (var row in rows)
            {
                var missing = fields.Where(f => section == "identificacion" ? !ProcedureEligibility.Valid(row, f.Key) : !SystemAnswers.Filled(row, f.Key)).ToList();
                if (section == "sistemas") missing = missing.Select(f => f.Key switch {
                    "sistema" when row?["sistema"]?.ToString() == "sii_v2" => ("moduloSiiId", "Módulo de SIIv2"),
                    "sistema" when row?["sistema"]?.ToString() == "otra" => ("sistemaOtro", "Nombre de la herramienta"),
                    "uso" when row?["uso"]?.ToString() == "otro" => ("usoOtro", "Describe el uso"),
                    _ => f
                }).ToList();
                if (section == "identificacion" && row?["prioridad"]?.ToString() is not ("1" or "2" or "3" or "4" or "5")
                    && missing.All(f => f.Key != "prioridad")) missing.Add(("prioridad", "prioridad vigente de 1 a 5"));
                if (missing.Count == 0) continue;
                var label = section == "sistemas" ? SystemAnswers.ToolLabel(row) : row?[fields[0].Key]?.ToString();
                if (string.IsNullOrWhiteSpace(label)) label = "Registro sin completar";
                pending.Add(new(section, section + ":" + row!["id"], missing[0].Key == "codigo" ? "validacion" : missing[0].Key,
                    $"{title} · {label}: completa {string.Join(", ", missing.Select(f => f.Label))}."));
            }
        }
        Rows("identificacion", "Identificación general", ProcedureEligibility.Required);
        Rows("sistemas", "Sistemas", ("sistema", "Sistema o herramienta"), ("uso", "¿Para qué se usa?"), ("estado", "¿Cómo funciona?"));
        foreach (var row in content["sistemas"]!.AsArray())
            if (!ProcedureEligibility.Contains(content, row?["proceso"]?.ToString()))
                pending.Add(new("sistemas", "sistemas:" + row!["id"], "proceso", $"Sistemas · {SystemAnswers.ToolLabel(row)}: completa y valida el procedimiento relacionado en Identificación general (Vigente o Ajustar)."));
        if (firstStage) return new(pending.Count == 0, pending);
        Rows("datos", "Datos", ("dato", "dato"), ("fuente", "fuente"), ("origen", "origen"));
        foreach (var row in content["identificacion"]!.AsArray())
        {
            var code = row!["codigo"]?.ToString();
            if (string.IsNullOrWhiteSpace(code)) continue;
            var evaluations = (content["evaluaciones"] as JsonObject)?[code] as JsonArray;
            for (var i = 0; i < 9; i++)
                if (evaluations?.ElementAtOrDefault(i)?["valor"]?.ToString() is not ("1" or "2" or "3" or "4" or "5"))
                    pending.Add(new("evaluacion", $"evaluaciones:{code}:{i}", "valor", $"Evaluación de {code}: completa el criterio {i + 1}."));
        }
        var questions = content["preguntas"]!.AsArray();
        for (var i = 0; i < questions.Count; i++)
            if (string.IsNullOrWhiteSpace(questions[i]?["respuesta"]?.ToString()))
                pending.Add(new("preguntas", $"preguntas:{i}", "respuesta", $"Preguntas · {i + 1}: {questions[i]?["pregunta"]}"));
        Rows("acuerdos", "Acuerdos", ("acuerdo", "acuerdo"), ("responsable", "responsable"), ("fecha", "fecha compromiso"));
        return new(pending.Count == 0, pending);
    }
}
