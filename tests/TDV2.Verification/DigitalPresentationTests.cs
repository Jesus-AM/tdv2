using System.Text.Json;
using System.Text.Json.Nodes;
using Tdv2.Competencias;
using Tdv2.Domain;

namespace Tdv2.Verification;

// Ejecutable de pruebas: proyecciones reales sin abrir conexiones ni ejecutar migraciones.
internal static class DigitalPresentationTests
{
    internal static void Run(string destination)
    {
        var checks = new List<string>();
        void Check(bool value, string name) { if (!value) throw new Exception(name); checks.Add(name); }
        var current = new DigitalInstrument(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts", "competencias-digitales-v1.json")));
        var generation = Guid.Parse("00000000-0000-0000-0000-000000000027");
        var scope = new DigitalScope(Fixtures.Profile("participante"), new(Fixtures.Units));
        var institution = scope.Institution();
        var profile = new JsonObject();
        foreach (var field in current.Resource["profile"]!.AsArray())
            profile[field!["id"]!.ToString()] = field["id"]!.ToString() == "funciones" ? new JsonArray(field["options"]![0]!.DeepClone()) : field["options"]![0]!.DeepClone();
        profile["puesto"] = "Personal operativo";
        DigitalSurvey Draft(JsonObject p, Dictionary<string, DigitalAnswer> a) => new() { InstrumentJson = current.Resource.ToJsonString(), ProfileJson = p.ToJsonString(), AnswersJson = JsonSerializer.Serialize(a, DigitalInstrument.Json) };
        DigitalSurveyView View(DigitalSurvey? s, bool accepted = true) => DigitalSurveyService.Project(current, s, institution, accepted, generation);
        var views = new Dictionary<string, DigitalSurveyView>();
        views["empty"] = View(null, false);
        views["profile"] = View(Draft(profile, []));
        views["initialized"] = View(Draft(profile, current.Blocks.SelectMany(b => b.Tools).ToDictionary(t => t.Id, _ => new DigitalAnswer("pending"))));
        foreach (var name in new[] { "empty", "profile", "initialized" })
            Check(!views[name].HasResponses && views[name].Evaluation.Report.Global is null && views[name].Evaluation.Report.Tools.All(t => t.Gap == 0), name + ": autocompletado/perfil/pending no producen resultados ni brechas");
        var first = current.Blocks[0].Tools.First(t => !t.Opt && !t.Doc);
        var zero = new Dictionary<string, DigitalAnswer> { [first.Id] = new("level", 0) };
        views["zero"] = View(Draft(profile, zero));
        Check(views["zero"].HasResponses && views["zero"].Evaluation.Report.Global == 0 && views["zero"].Evaluation.Report.Level == 0, "Inicial cero es resultado válido");
        var na = new Dictionary<string, DigitalAnswer> { [first.Id] = new("na") };
        views["na"] = View(Draft(profile, na));
        Check(views["na"].HasResponses && views["na"].Evaluation.Report.Global is null && views["na"].Evaluation.Report.Tools.Single(t => t.Id == first.Id).Value == -1, "No aplica real no se convierte en cero");
        var rubric = current.Blocks.Single(b => b.Id == "ciber").Tools.First();
        var rubricAnswers = new Dictionary<string, DigitalAnswer> { [rubric.Id] = new("level", Checks: "000000000000") };
        Check(View(Draft(profile, rubricAnswers)).Evaluation.Report.Global == 0, "No realizo ninguna es respuesta Inicial válida");
        var optional = current.Blocks[0].Tools.First(t => t.Opt && !t.Doc);
        var missing = views["zero"].Evaluation.Report.Tools.Single(t => t.Id == optional.Id);
        Check(missing.Value is null && missing.Gap == 0 && !views["zero"].Evaluation.Report.Route.Any(c => c.Tool == optional.Id), "Complementaria ausente no fabrica No utiliza, brecha ni curso");
        var optionalAnswers = new Dictionary<string, DigitalAnswer> { [optional.Id] = new("unused") };
        Check(View(Draft(profile, optionalAnswers)).HasResponses && View(Draft(profile, optionalAnswers)).Evaluation.Report.Tools.Single(t => t.Id == optional.Id).Value == -2, "No utiliza explícito conserva su significado");
        optionalAnswers[optional.Id] = new("pending");
        Check(!View(Draft(profile, optionalAnswers)).HasResponses && View(Draft(profile, optionalAnswers)).Evaluation.Pending.Contains(optional.Id), "Complementaria seleccionada pendiente no es respuesta real");
        var teacher = current.Blocks.SelectMany(b => b.Tools).First(t => t.Doc);
        Check(!View(Draft(profile, new() { [teacher.Id] = new("na") })).HasResponses, "Pregunta docente no aplicable no genera informe operativo");
        var partial = new Dictionary<string, DigitalAnswer>(zero);
        partial[current.Blocks[0].Tools.First(t => !t.Opt && !t.Doc && t.Id != first.Id).Id] = new("na");
        partial[optional.Id] = new("unused");
        partial[rubric.Id] = new("level", Checks: "000000000000");
        partial[current.Blocks.Single(b => b.Id == "datos").Tools.First().Id] = new("level", Checks: "111111110000");
        partial[current.Blocks.Single(b => b.Id == "ia").Tools.First(t => !t.Opt).Id] = new("level", 1);
        views["partial"] = View(Draft(profile, partial));
        var raw = current.Evaluate(profile, partial);
        Check(raw.Progress == views["partial"].Evaluation.Progress && raw.Pending.SequenceEqual(views["partial"].Evaluation.Pending) && raw.Report.Global == views["partial"].Evaluation.Report.Global, "Proyección preliminar conserva fórmula, pendientes y avance");
        var full = new Dictionary<string, DigitalAnswer>();
        foreach (var b in current.Blocks) foreach (var t in b.Tools.Where(t => !t.Doc))
            full[t.Id] = b.Id is "ciber" or "datos" or "sii" ? new("level", Checks: "111111111111") : new("level", 3);
        var evaluation = current.Evaluate(profile, full);
        Check(evaluation.Pending.Length == 0 && evaluation.Report.Global == 100 && evaluation.Report.Tools.All(t => t.Gap == 0), "Instrumento completo Avanzado mantiene 100 y ausencia de brechas");
        var final = Draft(profile, full);
        final.ReportJson = JsonSerializer.Serialize(evaluation.Report, DigitalInstrument.Json);
        final.CompletedAt = DateTimeOffset.Parse("2026-10-27T12:00:00Z");
        final.InstitutionJson = JsonSerializer.Serialize(institution with { Name = "Persona sintética · contexto histórico" }, DigitalInstrument.Json);
        var historical = JsonNode.Parse(final.InstrumentJson)!.AsObject();
        historical["notice"]!["version"] = "uacj-digital-aviso-2026-10-27.1";
        historical["notice"]!["paragraphs"] = new JsonArray("Texto anterior sintético");
        final.InstrumentJson = historical.ToJsonString();
        // Resultado persistido deliberadamente distinto de la evaluación actual: no debe recalcularse.
        var frozen = evaluation.Report with { Global = 42, Level = 1, Recommendations = [new("info", "Recomendación histórica", "Contenido histórico conservado sin recalcular.")] };
        final.ReportJson = JsonSerializer.Serialize(frozen, DigitalInstrument.Json);
        views["final"] = View(final);
        Check(JsonSerializer.Serialize(views["final"].Evaluation.Report, DigitalInstrument.Json) == final.ReportJson, "Informe definitivo usa exactamente JSON persistido");
        Check(views["final"].Institution["name"]!.ToString().Contains("histórico"), "Contexto institucional definitivo permanece histórico");
        Check(views["final"].Instrument["notice"]!["version"]!.ToString() == current.NoticeVersion && historical["notice"]!["version"]!.ToString().EndsWith(".1"), "Consulta muestra aviso vigente sin mutar instantánea anterior");
        views["historical"] = views["final"];
        // Para las capturas visuales, un informe completo coherente con sus cinco bloques.
        var index = 0;
        foreach (var b in current.Blocks) foreach (var t in b.Tools.Where(t => !t.Doc))
        {
            var level = index++ % 4;
            full[t.Id] = b.Id is "ciber" or "datos" or "sii" ? new("level", Checks: new string('1', level * 4).PadRight(12, '0')) : new("level", level);
        }
        final.AnswersJson = JsonSerializer.Serialize(full, DigitalInstrument.Json);
        final.ReportJson = JsonSerializer.Serialize(current.Evaluate(profile, full).Report, DigitalInstrument.Json);
        views["final"] = View(final);
        var oldDraft = Draft(profile, partial); oldDraft.InstrumentJson = historical.ToJsonString();
        views["oldConsent"] = View(oldDraft, false);
        Check(!views["oldConsent"].Accepted && views["oldConsent"].Instrument["notice"]!["version"]!.ToString() == "uacj-digital-aviso-2026-10-27.2", "Captura con aceptación anterior requiere aviso nuevo");
        Check(current.NoticeVersion == "uacj-digital-aviso-2026-10-27.2" && current.Resource["notice"]!["institutionalUrl"]!.ToString() == "https://www.uacj.mx/transparencia/aviso_privacidad.html", "Versión y enlace vigentes");
        var desired = new[] { 2, 3, 0, 1, 4 };
        Module[] Modules(int[] order) => order.Select(i => new Module(i + 10, "synthetic-" + i, DigitalRoutes.Names[i], DigitalRoutes.Base + "/" + DigitalRoutes.Slugs[i])).ToArray();
        var menus = new Dictionary<string, int[]>();
        foreach (var role in new[] { "administrador", "responsable_ur_supervisor", "responsable_ur", "participante" })
        {
            var s = new DigitalScope(Fixtures.Profile(role) with { Modules = Modules(desired) }, new(Fixtures.Units));
            menus[role] = s.MenuPages.ToArray();
            Check(s.MenuPages.SequenceEqual(role == "administrador" ? desired : role == "participante" ? [2, 3] : [2, 3, 0, 1]), "Orden Nexo y permisos: " + role);
        }
        var reversed = desired.Reverse().ToArray();
        Check(new DigitalScope(Fixtures.Profile("administrador") with { Modules = Modules(reversed) }, new(Fixtures.Units)).MenuPages.SequenceEqual(reversed), "Orden publicado distinto no se sustituye por orden fijo");
        Check(!new DigitalScope(Fixtures.Profile("administrador"), new(Fixtures.Units)).MenuPages.Any(), "Módulos TDV2 no conceden módulos de Competencias");
        var limited = new DigitalScope(Fixtures.Profile("administrador") with { Modules = Modules([3]) }, new(Fixtures.Units));
        Check(limited.MenuPages.SequenceEqual([3]), "Concesión exclusiva de informe se conserva");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        File.WriteAllText(destination, JsonSerializer.Serialize(new { views, menus, checks, database = false, migrations = false }, DigitalInstrument.Json));
        Console.WriteLine($"PASS {checks.Count} verificaciones de informe/aviso/menú; sin base ni migraciones. Proyecciones exportadas.");
    }
}
