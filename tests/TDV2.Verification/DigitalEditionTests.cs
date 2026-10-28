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
        Check(current.Blocks.Sum(b=>b.Tools.Length)==80 && current.Blocks.Single(b=>b.Id=="ciber").Tools.Length==old.Blocks.Single(b=>b.Id=="ciber").Tools.Length,"80 herramientas, Ciberseguridad intacto");
        Check(!complete.Report.Tools.Any(t=>retired.Contains(t.Id))&&!complete.Report.Route.Any(c=>retired.Contains(c.Tool))&&!complete.Pending.Intersect(retired).Any(),"Retiradas fuera de pendientes, resultados y capacitación");
        Check(JsonNode.DeepEquals(current.Resource["notice"],old.Resource["notice"]),"Aviso y versión de consentimiento intactos");
        var legacyProfile=profile.DeepClone().AsObject();legacyProfile["puesto"]="Personal operativo";legacyProfile["funciones"]=new JsonArray("Técnica","Académica");
        var legacyAnswers=new Dictionary<string,DigitalAnswer>(answers){[retired[0]]=new("level",3)};
        var draft=new DigitalSurvey{InstrumentVersion=old.Version,InstrumentJson=old.Resource.ToJsonString(),ProfileJson=legacyProfile.ToJsonString(),AnswersJson=JsonSerializer.Serialize(legacyAnswers,DigitalInstrument.Json),Version=5};
        var before=draft.AnswersJson;var institution=new DigitalScope(Fixtures.Profile("participante"),new(Fixtures.Units)).Institution();
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
        File.WriteAllText(destination,JsonSerializer.Serialize(new{checks,oldDraft=initial,currentDraft=view,academic=DigitalSurveyService.Project(current,new DigitalSurvey{InstrumentVersion=current.Version,InstrumentJson=current.Resource.ToJsonString(),ProfileJson=profile.ToJsonString(),AnswersJson=JsonSerializer.Serialize(answers,DigitalInstrument.Json)},institution,true,view.Generation)},DigitalInstrument.Json));
        Console.WriteLine($"PASS {checks.Count} verificaciones de edición y contrato de actividad sin base.");
    }
}
