using System.Text.Json.Nodes;
using Tdv2.Domain;
namespace Tdv2.Verification;

internal static class CaptureProgressTests
{
    internal static void Register(List<(string Name, Func<Task> Run)> cases, FormSchema schema)
    {
        void Check(bool value) { if (!value) throw new Exception("Avance y revisión no coinciden."); }
        void Test(string name, Action work) => cases.Add(("Avance/" + name, () => { work(); return Task.CompletedTask; }));
        JsonObject Complete()
        {
            var content = schema.Blank(Fixtures.Units[0]); var row = content["identificacion"]![0]!;
            row["codigo"] = "PO-01"; row["prioridad"] = "3"; row["validacion"] = "V";
            row["tramite"] = "Trámite"; row["resultado"] = "Constancia"; row["responsable"] = "Área"; row["usuario"] = new JsonArray("Docentes");
            var tool = content["sistemas"]![0]!; tool["proceso"] = "PO-01"; tool["sistema"] = "excel"; tool["uso"] = "consultar"; tool["estado"] = "bien";
            return content;
        }
        Test("captura vacía y sin requisitos no muestran 100 ni dividen entre cero", () =>
        {
            var blank = schema.Blank(Fixtures.Units[0]); var assessment = FormRequirements.Inspect(blank);
            Check(assessment.Percentage == 0 && !assessment.Review.listo && assessment.Review.pendientes.Count == 2);
            Check(FormRequirements.Rows(blank, "identificacion").Length == 0);
            Check(new CaptureAssessment([]).Percentage == 0 && !new CaptureAssessment([]).Review.listo);
            blank["identificacion"] = new JsonArray(); blank["sistemas"] = new JsonArray(); Check(FormCapture.Progress(blank) == 0);
        });
        Test("parcial cuenta campos válidos y no secciones visitadas, espacios ni texto arbitrario", () =>
        {
            var content = schema.Blank(Fixtures.Units[0]); content["identificacion"]![0]!["tramite"] = "Trámite";
            Check(FormCapture.Progress(content) == 12); content["identificacion"]![0]!["resultado"] = "   \n";
            Check(FormCapture.Progress(content) == 12); content["identificacion"]![0]!["usuario"] = new JsonArray("Inventado");
            Check(FormCapture.Progress(content) == 12);
        });
        Test("captura completa excluye opcionales y secciones posteriores sin significar envío", () =>
        {
            var content = Complete(); var a = FormRequirements.Inspect(content);
            Check(a.Total == 12 && a.Complete == 12 && a.Percentage == 100 && a.Review.listo);
            Check(FormStages.Describe(FormStages.Status(null, 2026), content).etapa.estado == "borrador");
            content["medioOtro"] = "Archivo"; content["sistemas"]![0]!["fallas"] = "Comentario";
            content["preguntas"]![0]!["respuesta"] = "Respuesta posterior"; Check(FormCapture.Progress(content) == 100);
        });
        Test("Egresados valida solo y combinado, conserva captura al recargar y completa avance/revisión", () =>
        {
            foreach (var recipients in new[] { new JsonArray("Egresados"), new JsonArray("Docentes", "Estudiantes", "Egresados") })
            {
                var content = Complete(); content["identificacion"]![0]!["usuario"] = recipients;
                var saved = schema.Validate(content, Fixtures.Units[0]);
                var reloaded = JsonNode.Parse(saved.ToJsonString())!.AsObject();
                Check(JsonNode.DeepEquals(reloaded["identificacion"]![0]!["usuario"], recipients));
                Check(ProcedureEligibility.Available(reloaded["identificacion"]![0]));
                Check(FormCapture.Progress(reloaded) == 100 && FormCapture.Review(reloaded).listo);
            }
            Check(!FormSchema.HasUsers(new JsonArray("Egresados", "Egresados")));
            Check(!FormSchema.HasUsers(new JsonArray("Egresados", "Opción inventada")));
        });
        Test("condicionales cuentan separados y desaparecen del cálculo al cambiar opción", () =>
        {
            var content = Complete(); var row = content["sistemas"]![0]!; row["sistema"] = "otra"; row["uso"] = "otro";
            var a = FormRequirements.Inspect(content); Check(a.Total == 14 && a.Complete == 11 && a.Percentage == 78);
            Check(a.Review.pendientes.Select(p => p.campo).Order().SequenceEqual(new[] { "sistemaOtro", "usoOtro", "captura" }.Order()));
            row["sistemaOtro"] = "Propia"; row["usoOtro"] = "Verificación"; Check(FormCapture.Progress(content) == 100);
            row["sistema"] = "excel"; row["uso"] = "consultar"; row["sistemaOtro"] = new string('x', 4001);
            Check(FormRequirements.Inspect(content).Total == 12 && FormCapture.Progress(content) == 100);
        });
        Test("SII exige ID válido e instantánea confirmada, sin invalidar históricos por disponibilidad", () =>
        {
            var content = Complete(); var row = content["sistemas"]![0]!; row["sistema"] = "sii_v2";
            foreach (var id in new[] { "", "  ", "ABC", "0", "1\n", new string('1', 41) })
            { row["moduloSiiId"] = id; row["moduloSiiDescripcion"] = "Módulo"; Check(FormCapture.Progress(content) == 84); }
            row["moduloSiiId"] = "123"; row["moduloSiiDescripcion"] = " "; Check(FormCapture.Progress(content) == 84);
            row["moduloSiiDescripcion"] = "Descripción conservada"; Check(FormCapture.Progress(content) == 100);
        });
        Test("módulos múltiples son un requisito; todos válidos, sin duplicados y colección vacía pendiente", () =>
        {
            var content = Complete(); var row = content["sistemas"]![0]!; row["sistema"] = "sii_v2";
            JsonObject Module(string id) => new() { ["id"] = id, ["descripcion"] = "Nombre completo" };
            row["modulosSii"] = new JsonArray(Module("1")); var one = FormRequirements.Inspect(content);
            Check(one.Total == 13 && one.Percentage == 100);
            row["modulosSii"]!.AsArray().Add(Module("2"));
            var several = FormRequirements.Inspect(content); Check(several.Total == one.Total && several.Percentage == 100);
            row["modulosSii"]!.AsArray().Add(Module("0")); Check(FormCapture.Progress(content) == 84);
            row["modulosSii"] = new JsonArray(Module("1"), Module("1")); Check(FormCapture.Progress(content) == 84);
            row["moduloSiiId"] = "1"; row["moduloSiiDescripcion"] = "Antiguo"; row["modulosSii"] = new JsonArray();
            Check(FormCapture.Progress(content) == 84 && FormCapture.Review(content).pendientes.Any(p => p.campo == "modulosSii"));
            row["sistema"] = "excel"; Check(FormCapture.Progress(content) == 100);
        });
        Test("ayuda inicial vacía no penaliza pero filas reales incompletas sí; eliminación recalcula", () =>
        {
            var content = Complete(); var rows = content["sistemas"]!.AsArray(); rows[0]!["id"] = "real";
            var helper = schema.Blank(Fixtures.Units[0])["sistemas"]![0]!.DeepClone(); rows.Add(helper);
            Check(FormCapture.Progress(content) == 100 && FormRequirements.Rows(content, "sistemas").Length == 1);
            helper["id"] = "agregada"; Check(FormCapture.Progress(content) == 75 && !FormCapture.Review(content).listo);
            rows.Remove(helper); Check(FormCapture.Progress(content) == 100);
        });
        Test("D/N, relaciones incompletas y respuestas históricas son pendientes sin convertir datos", () =>
        {
            var content = Complete(); content["identificacion"]![0]!["validacion"] = "D";
            var original = content.DeepClone(); Check(FormCapture.Progress(content) == 81);
            Check(FormCapture.Review(content).pendientes.Any(p => p.campo == "validacion") && FormCapture.Review(content).pendientes.Any(p => p.campo == "proceso"));
            Check(JsonNode.DeepEquals(content, original)); content = Complete(); content["sistemas"]![0]!["estado"] = "Parcial";
            Check(FormCapture.Progress(content) == 83 && FormCapture.Review(content).pendientes.Any(p => p.campo == "estado"));
            var previous = content.DeepClone().AsObject(); content["sistemas"]![0]!["fallas"] = "Corrección ajena";
            Check(schema.Validate(content, Fixtures.Units[0], previous: previous)["sistemas"]![0]!["estado"]!.ToString() == "Parcial");
        });
        Test("truncamiento nunca anuncia 100 con un requisito pendiente entre muchos", () =>
        {
            var content = Complete(); var rows = content["sistemas"]!.AsArray();
            for (var i = 1; i < 200; i++) { var row = rows[0]!.DeepClone(); row["id"] = "fila-" + i; rows.Add(row); }
            rows[199]!["estado"] = ""; Check(FormCapture.Progress(content) == 99 && !FormCapture.Review(content).listo);
        });
        Test("validación, serialización y recarga conservan porcentaje, revisión y resumen coherentes", () =>
        {
            var content = Complete(); content["sistemas"]![0]!["sistema"] = "otra";
            var saved = schema.Validate(content, Fixtures.Units[0]); var reloaded = JsonNode.Parse(saved.ToJsonString())!.AsObject();
            Check(FormSchema.Progress(saved) == FormStages.Progress(1, reloaded));
            var delivery = FormStages.Describe(FormStages.Status(null, 2026), reloaded);
            Check(!delivery.revision.listo && delivery.revision.pendientes.Any(p => p.campo == "sistemaOtro"));
            Check(delivery.resumen.procedimientos == 1 && delivery.resumen.herramientas == 1 && !delivery.secciones.Single(s => s.id == "sistemas").completa);
        });
        Test("cobertura por procedimiento sin filas ficticias y sin requisito genérico duplicado", () =>
        {
            var content = Complete(); var second = content["identificacion"]![0]!.DeepClone();
            second["id"] = "segundo"; second["codigo"] = "PO-02"; second["tramite"] = "Procedimiento dos";
            content["identificacion"]!.AsArray().Add(second);
            var a = FormRequirements.Inspect(content);
            Check(a.Total == 20 && a.Complete == 19 && a.Percentage == 95 && !a.Review.listo);
            Check(a.Review.pendientes.Single().bloque == "cobertura:segundo" && a.Coverage.Count == 2);
            Check(content["sistemas"]!.AsArray().Count == 1);
            var tool = content["sistemas"]![0]!.DeepClone(); tool["id"] = "otra"; tool["proceso"] = "PO-02";
            content["sistemas"]!.AsArray().Add(tool); a = FormRequirements.Inspect(content);
            Check(a.Total == 24 && a.Percentage == 100 && a.Review.listo);
            content["sistemas"]!.AsArray().RemoveAt(1); Check(FormCapture.Progress(content) == 95);
            content["sistemas"] = new JsonArray(); a = FormRequirements.Inspect(content);
            Check(a.Total == 16 && a.Complete == 14 && a.Review.pendientes.Count == 2);
            second["validacion"] = "N"; a = FormRequirements.Inspect(content);
            Check(!a.Coverage.Single(c => c.id == "segundo").aplicable && !a.Review.listo);
        });
        Test("estructura explícita compatible sin transformar lecturas ni confundir concurrencia", () =>
        {
            var legacy = Complete(); legacy.Remove(FormDocument.Field); var text = legacy.ToJsonString();
            var read = FormDocument.Read(text); Check(FormDocument.Version(read) == 0 && read.ToJsonString() == text);
            var normalized = schema.Validate(read, Fixtures.Units[0]); Check(FormDocument.Version(normalized) == 1);
            normalized.Remove(FormDocument.Field); Check(JsonNode.DeepEquals(legacy, normalized));
            foreach (var invalid in new JsonNode?[] { JsonValue.Create(2), JsonValue.Create("1"), null })
            {
                read[FormDocument.Field] = invalid;
                var rejected = false; try { FormDocument.Stamp(read); } catch (DomainProblem e) when (e.Status == 409) { rejected = true; }
                Check(rejected);
            }
        });
    }
}
