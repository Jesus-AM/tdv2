using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tdv2.Domain;

namespace Tdv2.Competencias;

public sealed record DigitalSummary(int Completed, int InProgress, int ParticipatingUnits, int? Target, int ConfiguredTargets, int TotalUnits, double? Participation, double? Maturity);
public sealed record DigitalUnitSummary(string Id, string Code, string Name, int Level, string? Parent, string? ParentName, int DirectCompleted, int DirectInProgress, DigitalSummary Summary);
public sealed record DigitalDistribution(string Id, string Name, int Count, int NotApplicable, int NotUsed, int[] Levels, double? Score);
public sealed record DigitalAggregatesResult(string? SelectedUnit, int? SelectedLevel, bool Institutional, DigitalUnitOption[] Roots, DigitalUnitOption[] Options, int[] Levels,
    DigitalSummary Summary, DigitalUnitSummary[] Units, DigitalDistribution[] Blocks, DigitalDistribution[] Tools, DigitalDistribution[] Positions,
    int[] CompetenceLevels, DigitalRecommendation[] Recommendations, string? SelectedPosition, string[] PositionOptions);

public sealed class DigitalAggregates(DigitalDbContext db, DigitalInstrument instrument)
{
    public async Task<DigitalAggregatesResult> Read(DigitalScope scope, string? unit, int? level, CancellationToken ct, string? position = null, bool finalizedOnly = false)
    {
        await using var read = await db.Database.BeginTransactionAsync(ct);
        await DigitalGeneration.Shared(db, ct);
        var ids = scope.Filter(unit); // Autorización antes de leer participantes, metas o resultados.
        var allowed = scope.Allowed;
        if (level is not null && (!scope.Institutional || !allowed.Any(u => u.LevelKnown && u.Level == level)))
            throw new DomainProblem(403, "El nivel no está disponible en este contexto.");
        if (level is not null)
        {
            var groups = allowed.Where(u => ids.Contains(u.Id) && u.Level == level).Select(u => u.Id).ToArray();
            ids = ids.Where(id => groups.Any(root => scope.Directory.Within(id, root))).ToArray();
        }
        var positionsAvailable = instrument.Resource["profile"]!.AsArray().Single(p => p!["id"]!.ToString() == "puesto")!["options"]!.AsArray().Select(p => p!.ToString()).ToArray();
        if (position is not null && !positionsAvailable.Contains(position)) throw new DomainProblem(422, "El tipo de puesto no pertenece al instrumento.");
        var query = db.Surveys.AsNoTracking().Where(s => s.UnitId != null && ids.Contains(s.UnitId));
        if (finalizedOnly) query = query.Where(s => s.CompletedAt != null);
        if (position is not null)
        {
            var filter = JsonSerializer.Serialize(new { puesto = position });
            query = query.Where(s => EF.Functions.JsonContains(s.ProfileJson, filter));
        }
        var surveys = await query
            .Select(s => new { s.Employee, s.UnitId, s.CompletedAt, s.ReportJson, s.ProfileJson }).ToListAsync(ct);
        var targets = await db.Targets.AsNoTracking().Where(t => ids.Contains(t.UnitId)).ToDictionaryAsync(t => t.UnitId, t => t.Value, ct);
        // Un participante tiene una sola fila. No se suman los resúmenes de padres e hijos.
        var reports = surveys.Where(s => s.CompletedAt is not null).ToDictionary(s => s.Employee, s => JsonSerializer.Deserialize<DigitalReport>(s.ReportJson!, DigitalInstrument.Json)!);
        DigitalSummary Summarize(string[] set)
        {
            var people = surveys.Where(s => set.Contains(s.UnitId)).DistinctBy(s => s.Employee).ToArray();
            var done = people.Count(s => s.CompletedAt is not null);
            var count = set.Count(targets.ContainsKey);
            var sum = set.Where(targets.ContainsKey).Sum(id => targets[id]);
            // Metas de adscripción DIRECTA, incluidas las unidades sin personal esperado (meta 0 explícita).
            // Una suma parcial nunca se presenta como el universo ni genera porcentaje.
            int? target = count == set.Length && set.Length > 0 ? sum : null;
            return new(done, people.Length - done, people.Select(p => p.UnitId).Distinct().Count(), target, count, set.Length,
                target > 0 && position is null ? done * 100d / target : null, DigitalInstrument.Mean(people.Where(p => p.CompletedAt is not null).Select(p => reports[p.Employee].Global)));
        }
        var rows = allowed.Where(u => ids.Contains(u.Id) && (level is null || u.Level == level)).OrderBy(u => u.Code).Select(u =>
        {
            var direct = surveys.Where(s => s.UnitId == u.Id).ToArray();
            return new DigitalUnitSummary(u.Id, u.Code, u.Description ?? u.Code, u.Level, u.Parent, scope.Directory.Get(u.Parent)?.Description,
                direct.Count(s => s.CompletedAt is not null), direct.Count(s => s.CompletedAt is null), Summarize(ids.Where(id => scope.Directory.Within(id, u.Id)).ToArray()));
        }).ToArray();
        var completed = reports.Values.ToArray();
        static DigitalDistribution Distribution(string id, string name, IEnumerable<double?> scores)
        {
            var values = scores.ToArray(); var levels = new int[4];
            foreach (var score in values) if (DigitalInstrument.ScoreLevel(score) is { } l) levels[l]++;
            return new(id, name, values.Count(v => v is not null), values.Count(v => v is null), 0, levels, DigitalInstrument.Mean(values));
        }
        var blocks = instrument.Blocks.Select(b => Distribution(b.Id, b.N, completed.Select(r => r.Blocks.SingleOrDefault(x => x.Id == b.Id)?.Score))).ToArray();
        var tools = instrument.Blocks.SelectMany(b => b.Tools).Select(t =>
        {
            var values = completed.SelectMany(r => r.Tools.Where(v => v.Id == t.Id)).ToArray(); var levels = new int[4];
            foreach (var v in values) if (v.Value >= 0) levels[v.Value.Value]++;
            return new DigitalDistribution(t.Id, t.N, levels.Sum(), values.Count(v => v.Value == -1), values.Count(v => v.Value == -2), levels,
                DigitalInstrument.Mean(values.Where(v => v.Value >= 0).Select(v => (double?)(v.Value!.Value / 3d * 100))));
        }).ToArray();
        var positions = surveys.Where(s => s.CompletedAt is not null).GroupBy(s => JsonNode.Parse(s.ProfileJson)!["puesto"]!.ToString())
            .Select(g => Distribution(g.Key, g.Key, g.Select(s => reports[s.Employee].Global))).ToArray();
        var global = Distribution("global", "Global", completed.Select(r => r.Global));
        var recommendations = new List<DigitalRecommendation>();
        if (completed.Length > 0)
        {
            var low = blocks.Where(b => b.Score is not null).OrderBy(b => b.Score).FirstOrDefault();
            if (low is not null) recommendations.Add(new("warning", "Priorizar el bloque de " + low.Name, $"{low.Levels[0] + low.Levels[1]} de {low.Count} personas evaluadas están en nivel Inicial o Básico. Abrir primero cursos básicos y dar seguimiento por dependencia."));
            var cyberIds = instrument.Blocks.Single(b => b.Id == "ciber").Tools.Select(t => t.Id).ToArray();
            var cyber = tools.Where(t => cyberIds.Contains(t.Id) && t.Count > 0).OrderByDescending(t => (double)t.Levels[0] / t.Count).Take(3).ToArray();
            if (cyber.Length > 0) recommendations.Add(new("warning", "Cerrar riesgos básicos de ciberseguridad", "Temas con mayor proporción en Inicial: " + string.Join(", ", cyber.Select(t => t.Name)) + ". Campaña breve de MFA, phishing y reporte de incidentes con taller práctico."));
            var iaIds = instrument.Blocks.Single(b => b.Id == "ia").Tools.Select(t => t.Id).ToArray();
            var gaps = tools.Where(t => !iaIds.Contains(t.Id) && t.Count >= Math.Max(1, completed.Length * .3)).OrderBy(t => t.Score).Take(5).ToArray();
            if (gaps.Length > 0) recommendations.Add(new("info", "Herramientas institucionales con mayor brecha", string.Join(", ", gaps.Select(t => t.Name)) + ". Verifica con la DGTI las licencias necesarias antes de programar cursos."));
            var foundations = tools.Where(t => iaIds.Take(4).Contains(t.Id) && t.Count > 0).ToArray();
            if (foundations.Length > 0) recommendations.Add(new("info", "Uso responsable de la IA antes que más herramientas", $"En promedio {foundations.Average(t => t.Levels[0] * 100d / t.Count):F1} % está en Inicial en los fundamentos. Difundir el Manual práctico de IA y el curso Uso responsable de la inteligencia artificial generativa."));
            var weakest = rows.Where(r => r.Summary.Maturity is not null).OrderBy(r => r.Summary.Maturity).FirstOrDefault();
            if (weakest is not null && rows.Count(r => r.Summary.Maturity is not null) > 1) recommendations.Add(new("info", $"Acompañamiento focalizado en {weakest.Code} - {weakest.Name}", "Planear capacitación por dependencia con metas trimestrales y un enlace de transformación digital."));
            recommendations.Add(new("success", "Formar una red de agentes de cambio", $"{global.Levels[3]} personas alcanzan nivel Avanzado global. Invitar a mentorías y compartir prácticas, plantillas y flujos."));
            recommendations.Add(new("info", "Dar seguimiento a la capacitación", "Revisar el avance de la ruta y los programas por dependencia. Esta encuesta admite una sola respuesta definitiva."));
        }
        return new(unit, level, scope.Institutional, scope.Roots.Select(DigitalUnitOption.From).ToArray(), allowed.Select(DigitalUnitOption.From).ToArray(), scope.Institutional ? allowed.Where(u => u.LevelKnown).Select(u => u.Level).Distinct().Order().ToArray() : [],
            Summarize(ids), rows, blocks, tools, positions, global.Levels, recommendations.ToArray(), position, positionsAvailable);
    }
    public static byte[] Csv(DigitalAggregatesResult data)
    {
        static string Cell(object? value)
        {
            var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            if (text.StartsWith('=') || text.StartsWith('+') || text.StartsWith('-') || text.StartsWith('@')) text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        var text = new StringBuilder("Tipo,Clave,Nombre,Finalizadas,En curso,Meta,Participación,Índice,Inicial,Básico,Intermedio,Avanzado,No aplica,No utiliza\r\n");
        void Row(params object?[] cells) => text.AppendLine(string.Join(',', cells.Select(Cell)));
        Row("Conjunto", data.SelectedUnit, "Participantes únicos" + (data.SelectedPosition is null ? "" : " · " + data.SelectedPosition), data.Summary.Completed, data.Summary.InProgress, data.Summary.Target, data.Summary.Participation, data.Summary.Maturity,
            data.CompetenceLevels[0], data.CompetenceLevels[1], data.CompetenceLevels[2], data.CompetenceLevels[3]);
        foreach (var u in data.Units) Row("UR y dependientes", u.Code, u.Name, u.Summary.Completed, u.Summary.InProgress, u.Summary.Target, u.Summary.Participation, u.Summary.Maturity);
        foreach (var (kind, rows) in new[] { ("Bloque", data.Blocks), ("Herramienta o tema", data.Tools), ("Tipo de puesto", data.Positions) })
            foreach (var r in rows) Row(kind, r.Id, r.Name, r.Count, null, null, null, r.Score, r.Levels[0], r.Levels[1], r.Levels[2], r.Levels[3], r.NotApplicable, r.NotUsed);
        foreach (var r in data.Recommendations) Row("Recomendación", "", r.Title + ": " + r.Text);
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(text.ToString())).ToArray();
    }
}
