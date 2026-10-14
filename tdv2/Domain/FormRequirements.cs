using System.Text.Json.Nodes;
namespace Tdv2.Domain;

public sealed record CaptureRequirement(string Section, string Block, string Field, string Message, bool Valid);
public sealed record ProcedureCoverage(string id, string codigo, string nombre, bool aplicable, bool completa,
    int registros, int validos, FormPending destino);
public sealed record CaptureAssessment(IReadOnlyList<CaptureRequirement> Requirements)
{
    public IReadOnlyList<ProcedureCoverage> Coverage { get; init; } = [];
    public int Total => Requirements.Count;
    public int Complete => Requirements.Count(r => r.Valid);
    public int Percentage => Total == 0 ? 0 : (int)(100L * Complete / Total);
    public FormReview Review => new(Total > 0 && Complete == Total, Requirements.Where(r => !r.Valid)
        .DistinctBy(r => (r.Section, r.Block, r.Field))
        .Select(r => new FormPending(r.Section, r.Block, r.Field, r.Message)).ToArray());
}

/// <summary>Requisitos aplicables de la captura vigente: una misma evaluación para avance y revisión.</summary>
public static class FormRequirements
{
    // Sólo la fila inicial intacta es ayuda de captura. Una fila agregada o procedente de inventario es un registro,
    // aunque siga incompleta. No se borra ni normaliza ninguna respuesta para calcular el avance.
    public static bool Placeholder(string section, JsonNode? row) => row is JsonObject value
        && value["id"]?.ToString() == "inicial"
        && value.All(p => p.Key == "id" || section == "identificacion" && p.Key == "fuente" && p.Value?.ToString() is "" or "Nuevo"
            || p.Value is null || p.Value is JsonArray { Count: 0 } || p.Value is JsonValue v && v.TryGetValue<string>(out var text) && string.IsNullOrWhiteSpace(text));
    public static JsonNode[] Rows(JsonObject content, string section) => (content[section] as JsonArray ?? [])
        .Where(r => r is not null && !Placeholder(section, r)).Select(r => r!).ToArray();

    public static CaptureAssessment Inspect(JsonObject content)
    {
        var requirements = new List<CaptureRequirement>();
        var procedures = Rows(content, "identificacion");
        var systems = Rows(content, "sistemas");
        var applicable = procedures.Where(ProcedureEligibility.Available).ToArray();
        foreach (var section in new[] { "identificacion", "sistemas" })
        {
            var title = section == "identificacion" ? "Identificación general" : "Sistemas y herramientas";
            var rows = Rows(content, section);
            // La cobertura por procedimiento sustituye la exigencia genérica de una herramienta.
            if (rows.Length == 0 && (section != "sistemas" || applicable.Length == 0))
            {
                var helper = (content[section] as JsonArray)?.FirstOrDefault();
                requirements.Add(new(section, helper is null ? section : section + ":" + helper["id"],
                    helper is null ? "" : section == "identificacion" ? "tramite" : "sistema", $"{title}: agrega y completa al menos un registro.", false));
            }
            foreach (var row in rows)
            {
                var index = content[section]!.AsArray().IndexOf(row) + 1;
                var code = row[section == "identificacion" ? "codigo" : "proceso"]?.ToString();
                var name = section == "identificacion" ? row["tramite"]?.ToString() : SystemAnswers.ToolLabel(row);
                var label = $"{title} · Registro {index}{(string.IsNullOrWhiteSpace(code) ? "" : " · " + code)} · {(string.IsNullOrWhiteSpace(name) ? "Registro sin completar" : name)}";
                void Add(string field, string caption, bool valid) => requirements.Add(new(section, section + ":" + row["id"], field, $"{label}: completa {caption}.", valid));
                if (section == "identificacion")
                {
                    foreach (var field in ProcedureEligibility.Required)
                        Add(field.Key == "codigo" ? "validacion" : field.Key,
                            field.Key == "validacion" && row["validacion"]?.ToString() is "D" or "N"
                                ? "validación pendiente de actualizar (Vigente o Ajustar)" : field.Label,
                            ProcedureEligibility.Valid(row, field.Key));
                }
                else
                {
                    Add("proceso", "el procedimiento relacionado en Identificación general (completo y válido, Vigente o Ajustar)", ProcedureEligibility.Contains(content, code));
                    Add("sistema", "Sistema o herramienta", SystemAnswers.Tools.Contains(row["sistema"]?.ToString()));
                    Add("uso", "¿Para qué se usa?", SystemAnswers.Uses.Contains(row["uso"]?.ToString()));
                    Add("estado", "¿Cómo funciona?", SystemAnswers.States.Contains(row["estado"]?.ToString()));
                    if (row["sistema"]?.ToString() == "sii_v2") Add(SystemModules.Field, "Módulos de SIIv2", SystemAnswers.Filled(row, "sistema"));
                    if (row["sistema"]?.ToString() == "otra") Add("sistemaOtro", "Nombre de la herramienta", SystemAnswers.ValidText(row, "sistemaOtro"));
                    if (row["uso"]?.ToString() == "otro") Add("usoOtro", "Describe el uso", SystemAnswers.ValidText(row, "usoOtro"));
                }
            }
        }
        var coverage = new List<ProcedureCoverage>();
        foreach (var procedure in procedures)
        {
            var id = procedure["id"]!.ToString(); var code = procedure["codigo"]?.ToString() ?? "";
            var name = procedure["tramite"]?.ToString() ?? "";
            var applies = ProcedureEligibility.Available(procedure);
            var linked = systems.Where(r => code.Length > 0 && r["proceso"]?.ToString() == code).ToArray();
            // Reutilizar exactamente los requisitos de las filas, incluidos sus condicionales.
            bool Valid(JsonNode row) => requirements.Where(r => r.Block == "sistemas:" + row["id"]).All(r => r.Valid);
            var valid = linked.Count(Valid);
            var missing = requirements.FirstOrDefault(r => !r.Valid && (applies
                ? linked.Any(row => r.Block == "sistemas:" + row["id"])
                : r.Block == "identificacion:" + id));
            var target = missing is not null ? new FormPending(missing.Section, missing.Block, missing.Field, missing.Message)
                : new FormPending("sistemas", linked.Length > 0 ? "sistemas:" + linked[0]["id"] : "cobertura:" + id,
                    linked.Length > 0 ? "sistema" : "captura", "");
            coverage.Add(new(id, code, name, applies, applies && valid > 0, linked.Length, valid, target));
        }
        foreach (var item in coverage.Where(c => c.aplicable))
            requirements.Add(new("sistemas", "cobertura:" + item.id, "captura",
                $"Sistemas y herramientas · {item.codigo} · {item.nombre}: registra al menos una herramienta completa y válida para este procedimiento.", item.completa));
        return new(requirements) { Coverage = coverage };
    }
}
