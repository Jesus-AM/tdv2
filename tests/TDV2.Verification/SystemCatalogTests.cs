using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Tdv2.Domain;
using Tdv2.Integrations.Catalogs;
using Tdv2.NativeVerification;
using Tdv2.Synchronization;
namespace Tdv2.Verification;

internal static class SystemCatalogTests
{
    internal static void Register(List<(string Name, Func<Task> Run)> cases, FormSchema schema)
    {
        void Check(bool value) { if (!value) throw new Exception("Contrato de módulos/herramientas incumplido."); }
        void Test(string name, Action work) => cases.Add(("Sistemas/" + name, () => { work(); return Task.CompletedTask; }));
        JsonObject Blank() => schema.Blank(Fixtures.Units[0]);
        JsonObject Validate(JsonObject value, JsonObject? old = null) => schema.Validate(value, Fixtures.Units[0], previous: old);
        void Reject(Action work) { try { work(); } catch (DomainProblem) { return; } catch (SyncProblem) { return; } throw new Exception("Se esperaba rechazo."); }
        Test("elegibilidad exige cada campo obligatorio vigente y V/A sin inventar requisitos", () =>
        {
            var row = new JsonObject { ["id"] = "confirmado", ["codigo"] = "PO-01", ["tramite"] = "Trámite", ["usuario"] = new JsonArray("Docentes"),
                ["resultado"] = "Entrega", ["responsable"] = "Área", ["validacion"] = "V", ["prioridad"] = "3" };
            Check(ProcedureEligibility.Available(row));
            foreach (var field in ProcedureEligibility.Required)
            {
                var incomplete = row.DeepClone(); incomplete[field.Key] = field.Key == "usuario" ? new JsonArray() : JsonValue.Create("");
                Check(!ProcedureEligibility.Available(incomplete));
            }
            foreach (var value in new[] { "D", "N", "desconocida" }) { row["validacion"] = value; Check(!ProcedureEligibility.Available(row)); }
            row["validacion"] = "A"; Check(ProcedureEligibility.Available(row)); row["prioridad"] = "17"; Check(!ProcedureEligibility.Available(row));
        });
        Test("guardar sólo primera etapa preserva campos posteriores sin validarlos como entrega", () =>
        {
            var content = Blank(); content["acuerdos"]![0]!["fecha"] = "Fecha histórica no normalizada";
            content["preguntas"]![0]!["respuesta"] = "Respuesta histórica";
            var original = content.DeepClone(); content["sistemas"]![0]!["fallas"] = "Comentario independiente";
            var result = schema.Validate(content, Fixtures.Units[0], new HashSet<string> { "sistemas:inicial" }, previous: original.AsObject());
            foreach (var section in new[] { "datos", "evaluaciones", "preguntas", "acuerdos" }) Check(JsonNode.DeepEquals(result[section], original[section]));
            Check(FormReview.Inspect(result).pendientes.All(p => p.seccion is "identificacion" or "sistemas"));
        });
        cases.Add(("Sistemas/lector real con ADO sintético: 1501 módulos, dos columnas y ninguna truncación ni filtro", async () =>
        {
            var sources = new SyntheticSources(); sources.Modules.Rows.Clear();
            for (var i = 1; i <= 1501; i++) sources.Modules.Rows.Add(i, i < 3 ? "Duplicado" : "Módulo " + i);
            var options = Options.Create(new SyncOptions()); var reader = new CatalogSource(sources, options);
            var rows = await reader.Read("sii_modulos", default); var snapshot = new CatalogPublication(options).Validate("sii_modulos", rows);
            Check(snapshot.Rows.Count == 1501 && snapshot.Rows.All(r => r.Count == 2));
            Check(snapshot.Rows[0]["desc_modulo"]!.ToString() == snapshot.Rows[1]["desc_modulo"]!.ToString());
            Check(snapshot.Rows[0]["id_modulo"]!.ToString() != snapshot.Rows[1]["id_modulo"]!.ToString());
            Check(sources.Queries.SequenceEqual(["SET TRANSACTION ISOLATION LEVEL READ COMMITTED", CatalogSource.SiiModulesSelect]));
            Check((await reader.Read("sii_modulos", default)).Count == 1501);
        }));
        cases.Add(("Sistemas/lectura interrumpida nunca devuelve una fracción; columnas incompletas y conexión fallida rechazan", async () =>
        {
            var sources = new SyntheticSources(); var reader = new CatalogSource(sources, Options.Create(new SyncOptions()));
            sources.FailAfter = 2;
            try { await reader.Read("sii_modulos", default); throw new Exception("Lectura parcial aceptada."); } catch (IOException) { }
            sources.FailAfter = -1; sources.Modules.Columns.Remove("DESC_MODULO");
            try { await reader.Read("sii_modulos", default); throw new Exception("Columnas incompletas aceptadas."); } catch (SyncProblem) { }
            sources.Failure = "sii_modulos";
            try { await reader.Read("sii_modulos", default); throw new Exception("Fallo aceptado."); } catch (IOException) { }
        }));
        cases.Add(("Sistemas/ID booleano no se convierte en identidad numérica", async () =>
        {
            var sources = new SyntheticSources(); sources.Modules.Rows[0][0] = true;
            var options = Options.Create(new SyncOptions()); var rows = await new CatalogSource(sources, options).Read("sii_modulos", default);
            Reject(() => new CatalogPublication(options).Validate("sii_modulos", rows));
        }));
        Test("IDs inválidos y duplicados incompatibles rechazan; idénticos se consolidan por ID", () =>
        {
            var publisher = new CatalogPublication(Options.Create(new SyncOptions()));
            JsonObject Module(string? id, string name = "Descripción") => new() { ["id_modulo"] = id, ["desc_modulo"] = name };
            foreach (var id in new string?[] { null, "", " ", "0", "-1", "1.5", "id", new('1', 41) }) Reject(() => publisher.Validate("sii_modulos", [Module(id)]));
            Reject(() => publisher.Validate("sii_modulos", [Module("1"), Module("1", "Distinto")]));
            Reject(() => publisher.Validate("sii_modulos", []));
            Check(publisher.Validate("sii_modulos", [Module("1"), Module("1")]).Rows.Count == 1);
            Check(publisher.Validate("sii_modulos", [Module("1"), Module("2")]).Rows.Count == 2);
        });
        Test("SII manual y automático expanden los dos catálogos independientemente de ILDA", () =>
        {
            Check(SyncCoordinator.Catalogs("sii").SequenceEqual(["sii", "sii_modulos"]));
            Check(SyncCoordinator.Catalogs("ambas").SequenceEqual(["sii", "sii_modulos", "ilda"]));
            Check(SyncCoordinator.Catalogs("ilda").SequenceEqual(["ilda"]));
        });
        Test("módulos reutilizan la conexión Sii a DesarrolloSII sin abrirla", () =>
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                { ["ConnectionStrings:Sii"] = "Server=127.0.0.1,59999;Database=DesarrolloSII;Integrated Security=true;Encrypt=true" }).Build();
            var connections = new SourceConnections(config);
            using var units = connections.Create("sii"); using var modules = connections.Create("sii_modulos");
            Check(units.ConnectionString == modules.ConnectionString && modules.Database == "DesarrolloSII");
            Check(modules.State == System.Data.ConnectionState.Closed);
        });
        Test("nuevos vacíos y SII sin módulo son borradores válidos pendientes", () =>
        {
            var content = Blank(); Check(FormSchema.Progress(Validate(content)) == 0);
            var row = content["sistemas"]![0]!; row["sistema"] = "sii_v2"; row["uso"] = "consultar"; row["estado"] = "bien";
            var without = Validate(content); Check(!SystemAnswers.Filled(without["sistemas"]![0], "sistema"));
            Check(FormCapture.Review(without).pendientes.Any(p => p.seccion == "sistemas" && p.campo == "moduloSiiId" && p.mensaje.Contains("Módulo de SIIv2")));
            row["moduloSiiId"] = "3"; row["moduloSiiDescripcion"] = "Consultas";
            Check(FormCapture.Progress(Validate(content)) > FormCapture.Progress(without));
        });
        Test("detalles estructurados sobreviven cambios accidentales y sólo cuentan si corresponden", () =>
        {
            var content = Blank(); var row = content["sistemas"]![0]!;
            row["sistema"] = "otra"; row["uso"] = "otro";
            Check(!SystemAnswers.Filled(row, "sistema") && !SystemAnswers.Filled(row, "uso"));
            row["sistemaOtro"] = "Herramienta propia"; row["usoOtro"] = "Verificar constancias";
            Check(SystemAnswers.Filled(row, "sistema") && SystemAnswers.Filled(row, "uso"));
            row["sistema"] = "sii_v2"; row["uso"] = "consultar";
            var saved = Validate(content)["sistemas"]![0]!;
            Check(saved["sistemaOtro"]!.ToString() == "Herramienta propia" && saved["usoOtro"]!.ToString() == "Verificar constancias");
            Check(!SystemAnswers.Filled(saved, "sistema") && SystemAnswers.Filled(saved, "uso"));
            row["sistema"] = "otra"; row["uso"] = "otro"; Check(SystemAnswers.Filled(Validate(content)["sistemas"]![0], "sistema"));
        });
        Test("varias herramientas por procedimiento y funcionamiento sin comentarios obligatorios", () =>
        {
            var content = Blank(); content["identificacion"]![0]!["codigo"] = "PO-01"; content["identificacion"]![0]!["validacion"] = "V";
            foreach (var field in new[] { "tramite", "resultado", "responsable" }) content["identificacion"]![0]![field] = "Respuesta completa";
            content["identificacion"]![0]!["usuario"] = new JsonArray("Docentes"); content["identificacion"]![0]!["prioridad"] = "3";
            var rows = content["sistemas"]!.AsArray(); rows.Clear();
            foreach (var state in SystemAnswers.States) rows.Add(new JsonObject { ["id"] = state, ["proceso"] = "PO-01", ["sistema"] = "excel", ["uso"] = "consultar", ["estado"] = state, ["fallas"] = "" });
            Check(Validate(content)["sistemas"]!.AsArray().Count == 4);
        });
        Test("históricos permanecen legibles y corregibles sin convertir valores ajenos", () =>
        {
            var original = Blank(); var row = original["sistemas"]![0]!; row["sistema"] = "Programa antiguo"; row["uso"] = "Uso anterior"; row["estado"] = "Parcial";
            var edited = original.DeepClone().AsObject(); edited["sistemas"]![0]!["fallas"] = "Comentario actualizado";
            var saved = Validate(edited, original)["sistemas"]![0]!;
            Check(saved["sistema"]!.ToString() == "Programa antiguo" && saved["estado"]!.ToString() == "Parcial");
            edited["sistemas"]![0]!["sistema"] = "excel"; Check(Validate(edited, original)["sistemas"]![0]!["uso"]!.ToString() == "Uso anterior");
            edited["sistemas"]![0]!["uso"] = "Inventado"; Reject(() => Validate(edited, original));
        });
        Test("códigos y orígenes ILDA/norma/Nuevo permanecen sin renumeración", () =>
        {
            var content = Blank(); var rows = content["identificacion"]!.AsArray(); rows.Clear();
            foreach (var (id, code, origin) in new[] { ("ilda:1", "PO-07", "ILDA"), ("norma", "PO-20", "ISO 21001"), ("manual", "PO-45", "Nuevo") })
            {
                var row = Blank()["identificacion"]![0]!.DeepClone(); row["id"] = id; row["codigo"] = code; row["fuente"] = origin; rows.Add(row);
            }
            Check(JsonNode.DeepEquals(Validate(content)["identificacion"], rows));
        });
    }
}
