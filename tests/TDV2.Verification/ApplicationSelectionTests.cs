using System.Text.Json.Nodes;
using Tdv2.Domain;

namespace Tdv2.Verification;

internal static class ApplicationSelectionTests
{
    internal static void Register(List<(string Name, Func<Task> Run)> cases, FormSchema schema)
    {
        void Check(bool value) { if (!value) throw new Exception("Contrato de aplicaciones incumplido."); }
        async Task Reject(Func<Task> work) { try { await work(); } catch (DomainProblem error) when (error.Status == 422) { return; } throw new Exception("Selección inválida aceptada."); }
        JsonArray Select(ApplicationKind kind, params string[] ids) => new(ids.Select(id => (JsonNode)new JsonObject
            { ["id"] = id, ["catalogo"] = kind.Key, ["descripcion"] = "Nombre del navegador" }).ToArray());
        cases.Add(("Aplicaciones/IDs int32 canónicos conservan el dominio del origen sin alias ni pérdida", () =>
        {
            foreach (var id in new[] { "0", "-2147483648", "2147483647", "34", "local:01234567-89ab-4cde-8123-0123456789ab" }) Check(SystemApplications.ValidId(id));
            foreach (var id in new[] { "", "01", "+1", " 1", "1.0", "1e2", "2147483648", "-2147483649", "-0", "local:123", "LOCAL:01234567-89ab-4cde-8123-0123456789ab" }) Check(!SystemApplications.ValidId(id));
            return Task.CompletedTask;
        }));
        foreach (var kind in SystemApplications.Kinds)
        {
            cases.Add(($"Aplicaciones/{kind.Key}: IDs, nombres autorizados, históricos y catálogo separado", async () =>
            {
                var row = new JsonObject { ["id"] = "fila", ["sistema"] = kind.Tool, [kind.Field] = Select(kind, "1", "2") };
                Task<string?> Catalog(string id) => Task.FromResult<string?>(id is "1" or "2" ? kind.Key + " " + id : null);
                await SystemApplications.Confirm(row, null, kind, Catalog);
                Check(row[kind.Field]![0]!["descripcion"]!.ToString() == kind.Key + " 1" && SystemApplications.Complete(row, kind));
                var previous = row.DeepClone(); row[kind.Field]![0]!["descripcion"] = "Intento de alterar instantánea";
                await SystemApplications.Confirm(row, previous, kind, _ => throw new Exception("No consultar históricos conservados"));
                Check(JsonNode.DeepEquals(row, previous));
                row[kind.Field] = Select(kind, "1", "1"); await Reject(() => SystemApplications.Confirm(row, previous, kind, Catalog));
                row[kind.Field] = Select(kind, "1", "99"); await Reject(() => SystemApplications.Confirm(row, previous, kind, Catalog));
                var other = SystemApplications.Kinds.Single(k => k != kind);
                row[kind.Field] = Select(other, "1"); await Reject(() => SystemApplications.Confirm(row, previous, kind, Catalog));
                row[kind.Field] = new JsonArray(); await SystemApplications.Confirm(row, previous, kind, Catalog);
                Check(!SystemApplications.Complete(row, kind) && SystemApplications.Read(row, kind).Count == 0);
                row[kind.Field] = Select(kind, "1"); row["sistema"] = "excel";
                await SystemApplications.Confirm(row, previous, kind, _ => throw new Exception("Detalle inactivo no consultado"));
                var dormant = row.DeepClone(); row["sistema"] = kind.Tool;
                await Reject(() => SystemApplications.Confirm(row, dormant, kind, _ => Task.FromResult<string?>(null)));
            }));
            cases.Add(($"Aplicaciones/{kind.Key}: un requisito condicional, cobertura y compatibilidad del JSON", () =>
            {
                var content = schema.Blank(Fixtures.Units[0]); var procedure = content["identificacion"]![0]!;
                procedure["codigo"] = "PO-01"; procedure["prioridad"] = "3"; procedure["tramite"] = "Procedimiento";
                procedure["resultado"] = "Resultado"; procedure["responsable"] = "Área"; procedure["usuario"] = new JsonArray("Docentes"); procedure["validacion"] = "V";
                var row = content["sistemas"]![0]!; row["proceso"] = "PO-01"; row["sistema"] = kind.Tool; row["uso"] = "consultar"; row["estado"] = "bien";
                var empty = FormRequirements.Inspect(content); Check(empty.Percentage < 100 && !empty.Review.listo);
                Check(empty.Requirements.Count(r => r.Field == kind.Field) == 1);
                row[kind.Field] = Select(kind, "1"); var single = FormRequirements.Inspect(content);
                Check(single.Percentage == 100 && single.Review.listo);
                row[kind.Field] = Select(kind, "1", "2"); var multiple = FormRequirements.Inspect(content);
                Check(multiple.Total == single.Total && multiple.Complete == single.Complete);
                var saved = schema.Validate(content, Fixtures.Units[0], previous: content);
                Check(JsonNode.DeepEquals(saved["sistemas"]![0]![kind.Field], row[kind.Field]));
                var oldClient = content.DeepClone().AsObject(); oldClient["sistemas"]![0]!.AsObject().Remove(kind.Field);
                Check(JsonNode.DeepEquals(schema.Validate(oldClient, Fixtures.Units[0], previous: content)["sistemas"]![0]![kind.Field], row[kind.Field]));
                row[kind.Field] = new JsonArray(); Check(!FormRequirements.Inspect(content).Review.listo);
                row["sistema"] = "excel"; Check(FormRequirements.Inspect(content).Review.listo);
                var blank = schema.Blank(Fixtures.Units[0]); Check(!blank["sistemas"]![0]!.AsObject().ContainsKey(kind.Field));
                return Task.CompletedTask;
            }));
        }
    }
}
