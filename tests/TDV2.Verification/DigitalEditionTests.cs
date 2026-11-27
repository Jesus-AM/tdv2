using System.Text.Json;
using System.Text.Json.Nodes;
using Tdv2.Competencias;
using Tdv2.Domain;
namespace Tdv2.Verification;

internal static class DigitalEditionTests
{
    internal static void Run(string destination)
    {
        var checks = new List<string>();
        void Check(bool value, string text) { if (!value) throw new Exception(text); checks.Add(text); }
        void Reject(Action work, string text) { try { work(); } catch(DomainProblem) { checks.Add(text); return; } throw new Exception(text); }
        var old = new DigitalInstrument(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts/competencias-digitales-v1.json")));
        var current = new DigitalInstrument(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Contracts/competencias-digitales-v2.json")));
        var retired = new[] { "m-bookings", "m-viva-connections-y-org-explo", "m-viva-learning", "m-seguridad-en-microsoft-365" };
        var profiles = current.Resource["positionRules"]!.AsObject();
        var names = new[] { "Dirección", "Subdirección", "Coordinación", "Jefatura de departamento", "Jefatura de función", "Responsable", "Personal Académico", "Asistente Profesional", "Apoyo Técnico", "Auxiliar administrativo", "Personal Manual" };
        Check(profiles.Select(p=>p.Key).SequenceEqual(names), "Once perfiles en orden exacto");
        var rules = new[] { "Dirección", "Subdirección", "Jefatura de función", "Jefatura de función", "Jefatura de función", "Jefatura de función", "Personal docente", "Personal operativo", "Personal operativo", "Personal operativo", "Personal operativo" };
        Check(names.Select(current.RulePosition).SequenceEqual(rules), "Equivalencias explícitas, Dirección/Subdirección separadas y Manual conservado");
        Check(current.Requirements("Dirección")["m-outlook"] == 3 && current.Requirements("Subdirección")["m-outlook"] == 1 && current.Requirements("Dirección")["m-word"] == 1 && current.Requirements("Subdirección")["m-word"] == 2, "Requisitos originales distintos: Outlook y Word, sin fusionar Dirección/Subdirección");
        var fields = current.Resource["profile"]!.AsArray();
        Check(fields.Single(f=>f!["id"]!.ToString()=="funciones")!["options"]!.AsArray().Select(o=>o!.ToString()).SequenceEqual(["Académica","Administrativa","Directiva","Otra"]), "Cuatro funciones exactas");
        var profile = new JsonObject();
        foreach(var f in fields)profile[f!["id"]!.ToString()]=f["multiple"]?.GetValue<bool>()==true?new JsonArray("Académica","Otra"):f["options"]![0]!.DeepClone();
        foreach(var (name, rule) in profiles)
        {
            profile["puesto"]=name;
            var expected=old.Requirements(rule!.ToString()).Where(p=>!retired.Contains(p.Key)).ToDictionary();
            Check(JsonSerializer.Serialize(current.Requirements(name).OrderBy(p=>p.Key))==JsonSerializer.Serialize(expected.OrderBy(p=>p.Key)), "Niveles esperados conservados: "+name);
            var evaluation=current.Evaluate(profile, new Dictionary<string,DigitalAnswer>());
            Check(!evaluation.Pending.Contains("perfil:funciones") && !evaluation.Pending.Contains("perfil:puesto"), "Perfil y selección múltiple válidos: "+name);
            Check(evaluation.Report.Tools.Any(t=>current.Blocks.SelectMany(b=>b.Tools).Single(k=>k.Id==t.Id).Doc)==(name=="Personal Académico"),"Aplicabilidad explícita: "+name);
        }
        profile["puesto"]="Personal Académico";
        profile["funciones"]=new JsonArray("Técnica");Reject(()=>current.ValidateProfile(profile),"Función Técnica rechazada");
        profile["funciones"]=new JsonArray("Atención o servicio");Reject(()=>current.ValidateProfile(profile),"Función Atención o servicio rechazada");
        profile["funciones"]=new JsonArray();Check(current.Evaluate(profile,new Dictionary<string,DigitalAnswer>()).Pending.Contains("perfil:funciones"),"Funciones obligatorias");
        profile["funciones"]=new JsonArray("Otra");current.ValidateProfile(profile);
        profile["puesto"]="Personal docente";Reject(()=>current.ValidateProfile(profile),"Perfil antiguo no aceptado por edición nueva");
        profile["puesto"]="Personal Académico";
        var answers=new Dictionary<string,DigitalAnswer>();
        foreach(var b in current.Blocks)foreach(var t in b.Tools)answers[t.Id]=b.Id is "ciber" or "datos" or "sii"?new("level",Checks:"000000000000"):new("level",0);
        var complete=current.Evaluate(profile,answers);
        Check(complete.Pending.Length==0&&complete.Progress==100&&complete.Report.Global==0,"Instrumento académico completo con cero válido");
        foreach (var block in current.Blocks)
        {
            var tool = block.Tools.First(t => !t.Opt && !t.Doc);
            var checklist = block.Id is "ciber" or "datos" or "sii";
            var trial = answers.ToDictionary();
            trial[tool.Id] = checklist ? new("outside", Checks: "110011111111") : new("outside");
            var partial = current.Evaluate(profile, trial);
            Check(partial.Pending.SequenceEqual([tool.Id]) && partial.Total == complete.Total && partial.Answered + 1 == complete.Answered,
                block.N + ": condición parcial cuenta un solo pendiente");
            for (var level = 1; level <= 3; level++)
            {
                var checksAtLevel = string.Concat(Enumerable.Range(1, 3).Select(i => i <= level ? "1110" : "0000"));
                trial[tool.Id] = checklist ? new("outside", Checks: checksAtLevel) : new("outside", level);
                var evaluated = current.Evaluate(profile, trial);
                var result = evaluated.Report.Tools.Single(t => t.Id == tool.Id);
                trial[tool.Id] = checklist ? new("level", Checks: checksAtLevel) : new("level", level);
                Check(evaluated.Report.Knowledge?.Score == current.Evaluate(profile, trial).Report.Global,
                    block.N + ": mismo peso del nivel ordinario " + level);
                Check(evaluated.Progress == 100 && evaluated.Report.Global == 0 && result is { OutsideRole: true, Value: null, Gap: 0 }
                    && result.OutsideLevel == level && !evaluated.Report.Route.Any(c => c.Tool == tool.Id), block.N + ": nivel adicional " + level);
            }
            trial[tool.Id] = checklist ? new("outside", 3, "100011111111") : new("outside", 0);
            Reject(() => current.Evaluate(profile, trial), block.N + ": nivel incompatible rechazado");
        }
        var extraOnly = new Dictionary<string, DigitalAnswer> { ["m-word"] = new("outside", 1) };
        var extraReport = current.Evaluate(profile, extraOnly).Report;
        Check(extraReport.Global is null && DigitalInstrument.HasResponses(extraReport, extraOnly),
            "Sólo conocimiento adicional da un informe preliminar sin puntaje del puesto");
        extraOnly["m-outlook"] = new("level", 3);
        Check(current.Evaluate(profile, extraOnly).Report.Global == 100, "Básico fuera de funciones no diluye una competencia aplicable Avanzada");
        var excel = new Dictionary<string, DigitalAnswer> { ["m-excel"] = new("outside", 3), ["m-outlook"] = new("na") };
        var excelReport = current.Evaluate(profile, excel).Report;
        Check(excelReport.Knowledge is { Version: 1, Score: 100 } && excelReport.Global is null
            && excelReport.Tools.Single(t => t.Id == "m-excel").Gap == 0 && !excelReport.Route.Any(c => c.Tool == "m-excel"),
            "Excel Avanzado fuera de funciones: 100 general, sin puntaje, brecha ni curso del puesto");
        excel["m-word"] = new("level", 0);
        excelReport = current.Evaluate(profile, excel).Report;
        Check(excelReport.Knowledge?.Score == 50 && excelReport.Global == 0 && excelReport.Tools.Single(t => t.Id == "m-word").Gap > 0,
            "Excel y Word: dos respuestas en el general, una en el puesto, la brecha de Word permanece");
        excel["m-excel"] = new("level", 3);
        Check(current.Evaluate(profile, excel).Report.Global == excelReport.Knowledge?.Score,
            "Excel tiene el mismo peso general que el nivel ordinario");
        excel["m-excel"] = new("outside");
        var partialExcel = current.Evaluate(profile, excel);
        Check(partialExcel.Report.Knowledge?.Score == 0 && partialExcel.Pending.Contains("m-excel"), "Parcial no puntúa y un cero real se conserva");
        excel.Remove("m-word");
        Check(current.Evaluate(profile, excel).Report.Knowledge?.Score is null, "No aplica y parcial dejan ambos índices sin puntaje");
        var frozen = excelReport with { Knowledge = null };
        var mixedMean = DigitalKnowledgeMean.From([(frozen, null), (excelReport, excelReport.Knowledge?.Score)]);
        Check(mixedMean == new DigitalKnowledgeMean(50, 1, 1), "Media mixta informa los históricos sin el indicador");
        var legacyTool = JsonSerializer.Deserialize<DigitalToolResult>("""{"id":"m-word","name":"Word","block":"m365","value":-1,"expected":2,"gap":0}""", DigitalInstrument.Json)!;
        Check(legacyTool is { Value: -1, OutsideRole: false, OutsideLevel: null } && OutsideCounts.Count([legacyTool]) == new OutsideCounts(0, 0, 0),
            "Un No aplica histórico no declara un nivel adicional");
        Check(DigitalInstrument.ChecklistLevel("110011111111") == 0 && DigitalInstrument.ChecklistLevel("111011001111") == 1 && DigitalInstrument.ChecklistLevel("111011101110") == 3, "Tres de cuatro por nivel, sin saltar el anterior");
        var rubric = current.Blocks.Single(b => b.Id == "ciber").Tools.Take(2).ToArray();
        answers[rubric[0].Id] = new("level", Checks:"100000000000"); answers[rubric[1].Id] = new("level", Checks:"110000000000");
        var separate = current.Evaluate(profile, answers);
        Check(separate.Report.Tools.Where(t => rubric.Any(r => r.Id == t.Id)).All(t => t.Value == 0), "No suma actividades de rúbricas distintas");
        answers[rubric[0].Id] = new("na"); answers.Remove(rubric[1].Id);
        var distinct = current.Evaluate(profile, answers);
        Check(distinct.Report.Tools.Single(t => t.Id == rubric[0].Id) is { Value: -1, Gap: 0 } && distinct.Pending.Contains(rubric[1].Id) && !distinct.Pending.Contains(rubric[0].Id), "No aplica excluido del puntaje y pendiente real separado");
        Check(!DigitalAggregates.Started("{}", "{}") && !DigitalAggregates.Started("{}", "{\"x\":{\"state\":\"pending\"}}") && DigitalAggregates.Started("{}", "{\"x\":{\"state\":\"level\",\"level\":0}}"), "Inicio requiere captura real; cero sí cuenta");
        foreach (var t in rubric) answers[t.Id] = new("level", Checks:"000000000000");
        Check(current.Blocks.Sum(b=>b.Tools.Length)==80 && current.Blocks.Single(b=>b.Id=="ciber").Tools.Length==old.Blocks.Single(b=>b.Id=="ciber").Tools.Length,"80 herramientas, Ciberseguridad intacto");
        Check(!complete.Report.Tools.Any(t=>retired.Contains(t.Id))&&!complete.Report.Route.Any(c=>retired.Contains(c.Tool))&&!complete.Pending.Intersect(retired).Any(),"Retiradas fuera de pendientes, resultados y capacitación");
        Check(JsonNode.DeepEquals(current.Resource["notice"],old.Resource["notice"]),"Aviso y versión de consentimiento intactos");
        var legacyProfile=profile.DeepClone().AsObject();legacyProfile["puesto"]="Personal operativo";legacyProfile["funciones"]=new JsonArray("Técnica","Académica");
        var legacyAnswers=new Dictionary<string,DigitalAnswer>(answers){[retired[0]]=new("level",3)};
        var draft=new DigitalSurvey{InstrumentVersion=old.Version,InstrumentJson=old.Resource.ToJsonString(),ProfileJson=legacyProfile.ToJsonString(),AnswersJson=JsonSerializer.Serialize(legacyAnswers,DigitalInstrument.Json),Version=5};
        var before=draft.AnswersJson;var institution=new DigitalScope(Fixtures.Profile("participante"),new(Fixtures.Units)).Institution();
        var clipchamp = VerifyClipchamp(current, old, institution, profile, answers, Check);
        var initial=DigitalSurveyService.Project(current,draft,institution,true,Guid.NewGuid());
        Check(initial.EditionUpdate?.ReviewFields.Length==2 && draft.AnswersJson==before && draft.Version==5,"Lectura advierte sin modificar borrador");
        DigitalEditions.Upgrade(current,draft,DateTimeOffset.UtcNow);
        var view=DigitalSurveyService.Project(current,draft,institution,true,initial.Generation);
        Check(view.EditionUpdate is null&&!view.Profile.ContainsKey("puesto")&&view.Profile["funciones"]!.AsArray().Select(f=>f!.ToString()).SequenceEqual(["Académica"]), "Sin clasificación silenciosa, conserva funciones válidas");
        Check(view.Answers.Count==80&&!view.Answers.ContainsKey(retired[0])&&view.Navigation.Resume=="perfil","Respuestas vigentes conservadas y vuelta a Perfil");
        var archive=JsonNode.Parse(draft.InstrumentJson)!["previousDraft"]!;
        Check(JsonNode.DeepEquals(archive["answers"],JsonNode.Parse(before))&&!view.Instrument.ContainsKey("previousDraft"),"Archivo anterior exacto sólo en persistencia");
        draft.CompletedAt=DateTimeOffset.UtcNow;Reject(()=>DigitalEditions.Upgrade(current,draft,DateTimeOffset.UtcNow),"Definitiva no admite actualización");
        var payload=DigitalActivity.Payload(Guid.NewGuid(),DateTimeOffset.UtcNow,"actor@uacj.mx","Actor","informe","consultar_representado","informe","5","otra@uacj.mx");
        Check(payload["actor_email"]!.ToString()!=payload["usuario_afectado_email"]!.ToString() && !payload.Any(p=>p.Key.Contains("token")||p.Key is "antes" or "despues"),"Actividad distingue actor y afectado sin respuestas ni tokens");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        File.WriteAllText(destination,JsonSerializer.Serialize(new{checks,clipchamp,oldDraft=initial,currentDraft=view,academic=DigitalSurveyService.Project(current,new DigitalSurvey{InstrumentVersion=current.Version,InstrumentJson=current.Resource.ToJsonString(),ProfileJson=profile.ToJsonString(),AnswersJson=JsonSerializer.Serialize(answers,DigitalInstrument.Json)},institution,true,view.Generation)},DigitalInstrument.Json));
        Console.WriteLine($"PASS {checks.Count} verificaciones de edición y contrato de actividad sin base.");
    }

    private static object VerifyClipchamp(DigitalInstrument current, DigitalInstrument old, DigitalInstitution institution,
        JsonObject profile, Dictionary<string, DigitalAnswer> answers, Action<bool, string> check)
    {
        const string id = "m-clipchamp-y-stream";
        static JsonNode Card(JsonObject resource) => resource["modules"]!.AsArray().Single(b => (string?)b!["id"] == "m365")!["tools"]!
            .AsArray().Single(t => (string?)t!["id"] == id)!;
        var generation = Guid.NewGuid();
        DigitalSurveyView View(DigitalSurvey? survey) => DigitalSurveyService.Project(current, survey, institution, true, generation, "m365");

        var original = Card(old.Resource);
        var expected = original.DeepClone();
        expected["n"] = "Clipchamp";
        expected["s"] = "Clipchamp";
        const string statistics = "Consultar las estadísticas de visualización de un video de capacitación en Clipchamp";
        expected["lv"]![2] = new JsonArray(
            "Usar voz generada a partir de texto para narrar un video",
            "Exportar videos en MP4 con la resolución y la relación de aspecto adecuadas para el canal de publicación",
            "Agregar formularios o cuestionarios interactivos a un video en Stream",
            statistics);
        expected["lic"] = "La disponibilidad de algunas funciones depende de la licencia y de la configuración institucional.";
        var fresh = View(null);
        check(JsonNode.DeepEquals(Card(fresh.Instrument), expected) && (string?)fresh.Instrument["version"] == "uacj-digital-2026-10-28.1",
            "Encuesta nueva: tarjeta de Clipchamp en la edición vigente");

        var snapshot = current.Resource.DeepClone().AsObject();
        Card(snapshot).ReplaceWith(original.DeepClone());
        Card(snapshot)["lv"]![0]![0] = "Actividad básica propia del borrador sintético";
        var captured = new Dictionary<string, DigitalAnswer>(answers) { [id] = new("level", 2) };
        var survey = new DigitalSurvey { InstrumentVersion = current.Version, InstrumentJson = snapshot.ToJsonString(),
            ProfileJson = profile.ToJsonString(), AnswersJson = JsonSerializer.Serialize(captured, DigitalInstrument.Json), Version = 7 };
        var stored = JsonSerializer.Serialize(survey);
        var draft = View(survey);
        expected["lv"]![0]![0] = Card(snapshot)["lv"]![0]![0]!.DeepClone();
        check(JsonNode.DeepEquals(Card(draft.Instrument), expected) && draft.Answers[id] == captured[id] && draft.EditionUpdate is null
            && draft.Version == 7 && JsonSerializer.Serialize(survey) == stored,
            "Borrador: muestra Clipchamp y conserva su actividad básica, nivel Intermedio y copia guardada");

        var misplaced = snapshot.DeepClone().AsObject();
        Card(misplaced)["lv"]![2]![2] = statistics;
        var corrected = Card(View(new DigitalSurvey { InstrumentVersion = current.Version, InstrumentJson = misplaced.ToJsonString(),
            ProfileJson = survey.ProfileJson, AnswersJson = survey.AnswersJson }).Instrument);
        check(JsonNode.DeepEquals(corrected, expected)
            && corrected["lv"]!.AsArray().SelectMany(level => level!.AsArray()).Count(activity => (string?)activity == statistics) == 1,
            "Borrador con estadísticas en la tercera viñeta: recupera los formularios y muestra estadísticas una sola vez, en la cuarta");

        var previous = new DigitalInstrument(survey.InstrumentJson);
        var report = previous.Evaluate(profile, captured).Report;
        survey.ReportJson = JsonSerializer.Serialize(report, DigitalInstrument.Json);
        survey.CompletedAt = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        stored = JsonSerializer.Serialize(survey);
        var delivered = View(survey);
        check(delivered.CompletedAt == survey.CompletedAt && JsonNode.DeepEquals(delivered.Instrument, snapshot)
            && JsonSerializer.Serialize(delivered.Answers, DigitalInstrument.Json) == survey.AnswersJson
            && JsonSerializer.Serialize(delivered.Evaluation.Report, DigitalInstrument.Json) == survey.ReportJson
            && JsonSerializer.Serialize(survey) == stored, "Entregada: conserva instrumento, respuestas, fecha, puntajes e informe definitivo");
        var newSurvey = new DigitalSurvey { InstrumentVersion = current.Version, InstrumentJson = current.Resource.ToJsonString(), ProfileJson = profile.ToJsonString() };
        return new { fresh = View(newSurvey), draft, delivered };
    }
}
