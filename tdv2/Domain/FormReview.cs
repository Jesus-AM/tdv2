using System.Text.Json.Nodes;
namespace Tdv2.Domain;

public sealed record FormPending(string seccion, string bloque, string campo, string mensaje);
public sealed record FormReview(bool listo, IReadOnlyList<FormPending> pendientes)
{
    /// <summary>La misma revisión alimenta los enlaces de captura y la autorización final del envío.</summary>
    public static FormReview Inspect(JsonObject content, bool firstStage = true)
    {
        if (firstStage) return FormRequirements.Inspect(content).Review;
        var pending = FormRequirements.Inspect(content).Review.pendientes.ToList();
        void Rows(string section, string title, params (string Key, string Label)[] fields)
        {
            var rows = content[section]!.AsArray();
            if (rows.Count == 0) pending.Add(new(section, section, "", $"{title}: agrega al menos un registro."));
            var index = 0;
            foreach (var row in rows)
            {
                index++;
                var missing = fields.Where(f => !SystemAnswers.Filled(row, f.Key)).ToList();
                if (missing.Count == 0) continue;
                var label = row?[fields[0].Key]?.ToString();
                if (string.IsNullOrWhiteSpace(label)) label = "Registro sin completar";
                foreach (var field in missing)
                    pending.Add(new(section, section + ":" + row!["id"], field.Key,
                        $"{title} · Registro {index}{(row?["codigo"]?.ToString() is { Length: > 0 } code ? " · " + code : "")} · {label}: completa {field.Label}."));
            }
        }
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
        Rows("acuerdos", "Acuerdos", ("acuerdo", "acuerdo"), ("responsable", "responsable"), ("fecha", "fecha de compromiso"));
        return new(pending.Count == 0, pending);
    }
}
