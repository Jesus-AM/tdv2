using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Tdv2.Domain;
using Tdv2.Infrastructure;
namespace Tdv2.Verification;

internal static class EntryPoint
{
    private static readonly List<(string Name, Func<Task> Run)> Cases = [];
    private static void Check(bool condition, string message = "La condición esperada no se cumplió.") { if (!condition) throw new Exception(message); }
    private static void Test(string name, Action run) => Cases.Add((name, () => { run(); return Task.CompletedTask; }));
    private static void Http(string name, Func<Application, HttpClient, Task> run) => Cases.Add((name, async () =>
    { await using var app = new Application(); using var client = app.Client(); await run(app, client); }));
    private static void Invalid(Action run, int status = 422)
    {
        try { run(); } catch (DomainProblem error) when (error.Status == status) { return; }
        throw new Exception($"Se esperaba rechazo {status}.");
    }
    private static async Task<JsonObject> Json(HttpResponseMessage response) => (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    private static async Task Csrf(HttpClient client)
    {
        var json = await Json(await client.GetAsync("/session/csrf"));
        client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", json["token"]!.GetValue<string>());
    }
    public static async Task<int> Main()
    {
        var directory = new UnitDirectory(Fixtures.Units);
        var access = new FormAccess(directory);
        var path = Path.Combine(AppContext.BaseDirectory, "Contracts", "procesos_operativos.json");
        var schema = new FormSchema(File.ReadAllText(path));
        JsonObject Blank() => schema.Blank(Fixtures.Units[0]);
        void Reject(string name, Action<JsonObject> mutate) => Test(name, () => { var data = Blank(); mutate(data); Invalid(() => schema.Validate(data, Fixtures.Units[0])); });

        Test("UR inferior comparte formato nivel 3", () => Check(directory.FormUnit("A4")?.Id == "A3"));
        Test("UR inactivas y ausentes no autorizan", () => Check(directory.Get("inactive") is null && directory.Get("absent") is null));
        Test("Ciclo no inventa ancestro ni entra en bucle", () => { var d = new UnitDirectory([new("x", "x", "x", 4, "y", 2026), new("y", "y", "y", 4, "x", 2026)]); Check(d.LevelTwo("x") is null && !d.Within("x", "z")); });
        Test("Responsable usa empleado y mantiene responsabilidades múltiples", () => { var s = access.Scopes(Fixtures.Profile("responsable_ur")); Check(s["A"].Edit && s["B3"].Edit && s["A3"].Edit && !s.ContainsKey("B")); });
        Test("Adscripción no convierte en responsable", () => Check(access.Scopes(Fixtures.Profile("responsable_ur") with { User = Fixtures.Profile().User with { Employee = "9999" } }).Count == 0));
        Test("Ceros iniciales no equivalen a número entero", () => Check(access.Scopes(Fixtures.Profile("responsable_ur") with { User = Fixtures.Profile().User with { Employee = "1" } }).Count == 0));
        Test("Consulta institucional sólo agrega lectura", () => Check(access.Scopes(Fixtures.Profile("consulta_institucional")).Values.All(s => !s.Edit)));
        Test("Nuevo responsable institucional funciona solo, sin facultades administrativas", () =>
        {
            var profile = Fixtures.Profile("responsable_ur_institucional"); var scopes = access.Scopes(profile);
            Check(scopes["A"].Edit && scopes["B3"].Edit && !scopes["B"].Edit && scopes["A3"].Edit);
            Check(scopes["A"].Own && !scopes["B"].Own);
            Check(!ModuleAccess.Allows(profile, "configuracion") && !ModuleAccess.Allows(profile, "sincronizaciones") && !ModuleAccess.Allows(profile, "pruebas_acceso"));
        });
        Test("Tipo N conserva ascendientes pero no autoriza formatos ni responsabilidad", () =>
        {
            var units = new UnitDirectory([new("A", "06000", "Principal", 2, null, 2026, "0001", Kind: "1"),
                new("aux", "6001", "Auxiliar", 3, "A", 2026, "0001", Kind: " n "),
                new("child", "6002", "Dependiente", 3, "aux", 2026, Kind: "1")]);
            Check(units.Get("aux") is not null && units.Within("child", "A") && units.DirectoryParent(units.Get("child")!) == "A");
            Check(!UnitDirectory.IsForm(units.Get("aux")!) && units.Responsibilities(Fixtures.Profile().User).Count() == 1);
        });
        Test("Tipo N normalizado se excluye; tipo 0 admite formatos sólo en niveles 2 y 3", () =>
        {
            foreach (var kind in new string?[] { "N", "n", " N ", "\t n\r\n", "0", " 0 ", "00", "1", "NA", "", null })
                foreach (var level in new[] { 1, 2, 3, 4 })
                {
                    var unit = new Unit("tipo", "06000", "Unidad sintética", level, null, 2026, Kind: kind);
                    var excluded = kind?.Trim().Equals("N", StringComparison.OrdinalIgnoreCase) == true;
                    Check(UnitDirectory.IsEligible(unit) == !excluded);
                    Check(UnitDirectory.IsForm(unit) == (!excluded && level is 2 or 3));
                    Check(unit.Kind == kind && unit.Code == "06000");
                }
            var zero = new Unit("zero", "06000", "Principal tipo 0", 2, null, 2026, "0001", Kind: "0");
            var child = new Unit("child", "06001", "Dependiente tipo 0", 3, "zero", 2026, Kind: "0");
            var units = new UnitDirectory([zero, child, new("aux", "99", "Auxiliar", 3, "zero", 2026, Kind: "N")]);
            foreach (var role in new[] { "administrador", "responsable_ur", "responsable_ur_supervisor", "consulta_institucional" })
            {
                var profile = Fixtures.Profile(role) with { User = Fixtures.Profile().User with { UnitId = "child" } };
                var scopes = new FormAccess(units).Scopes(profile);
                Check(scopes.Keys.Order().SequenceEqual(new[] { "child", "zero" }));
                Check(scopes.Values.All(scope => scope.Edit == (role != "consulta_institucional")));
            }
        });
        Test("Administrador combinado no rebasa rama", () => { var s = access.Scopes(Fixtures.Profile("administrador", "responsable_ur", "colaborador_dependencias")); Check(s["A"].Edit && s["A3"].Edit && !s["B"].Edit && !s["B3"].Edit); });
        Test("Participación recalcula central sin trasladar delegaciones ni ampliar autoridad", () =>
        {
            var units = new UnitDirectory(Fixtures.Units, new(2, [2,4], ["N"]));
            var local = Fixtures.Profile("colaborador_local");
            var central = new Grant(900, local.Roles[0].Id, "A4", "central");
            Check(new FormAccess(units).Scopes(local, grants: [central]).Keys.Single() == "A4");
            var delegated = new Collaboration(local.User.Email, "0001", "A4", "A3", "local", 901, local.Roles[0].Id, "A");
            Check(new FormAccess(units).Scopes(local, [delegated], [new(901, local.Roles[0].Id, "A4")]).Count == 0);
            Check(new FormAccess(units).Scopes(local, [delegated with { Revoked = true }], [new(901, local.Roles[0].Id, "A4")]).Count == 0);
            var dependent = Fixtures.Profile("colaborador_dependencias");
            Check(new FormAccess(units).Scopes(dependent, grants: [central]).Keys.Order().SequenceEqual(new[] { "A", "A4" }));
            var responsible = new FormAccess(units).Scopes(Fixtures.Profile("responsable_ur_supervisor"));
            Check(responsible["A4"].Edit && !responsible["B"].Edit);
            var root = new Unit("superior", "1", "Superior", 1, null, 2026);
            var boundary = new UnitDirectory([root, ..Fixtures.Units.Select(u => u.Id == "A" ? u with { Parent = root.Id } : u)], new(3, [1], []));
            Check(new FormAccess(boundary).Scopes(local, grants: [central]).Count == 0);
        });
        Test("Administrador ambiguo conserva lectura sin edición", () => Check(access.Scopes(Fixtures.Profile("administrador") with { User = Fixtures.Profile().User with { Origin = "multiple", UnitId = null } }).Values.All(s => !s.Edit)));
        Test("Administrador sin empleado no edita", () => Check(access.Scopes(Fixtures.Profile("administrador") with { User = Fixtures.Profile().User with { Employee = null } }).Values.All(s => !s.Edit)));
        var collaborator = Fixtures.Profile("colaborador_local");
        var link = new Collaboration(collaborator.User.Email, "0001", "A4", "A3", "local", 50, 1, "A");
        var grant = new Grant(50, 1, "A4");
        Test("Colaborador delegado requiere vínculo y concesión de aplicación", () => { Check(access.Scopes(collaborator, [link], [grant])["A3"].Edit); Check(access.Scopes(collaborator, [link], []).Count == 0); });
        Test("Cambio de empleado invalida colaboración", () => Check(access.Scopes(collaborator with { User = collaborator.User with { Employee = "0002" } }, [link], [grant]).Count == 0));
        Test("Cambio de adscripción invalida colaboración", () => Check(access.Scopes(collaborator with { User = collaborator.User with { UnitId = "B3" } }, [link], [grant]).Count == 0));
        Test("Rol revocado invalida colaboración", () => Check(access.Scopes(collaborator with { Roles = [] }, [link], [grant]).Count == 0));
        Test("Revocación local deniega aunque concesión siga vigente", () => Check(access.Scopes(collaborator, [link with { Revoked = true }], [grant]).Count == 0));
        Test("Concesión delegada ajena no amplía alcance", () => Check(access.Scopes(collaborator, [link with { Scope = "B3" }], [grant]).Count == 0));
        Test("Central local sin vínculo usa adscripción vigente de niveles 2, 3 e inferiores", () =>
        {
            foreach (var (origin, form) in new[] { ("A", "A"), ("A3", "A3"), ("A4", "A3"), ("B3", "B3") })
            {
                var p = collaborator with { User = collaborator.User with { UnitId = origin } };
                var scopes = access.Scopes(p, grants: [grant with { Origin = "central" }]);
                Check(scopes.Count == 1 && scopes[form].Edit && !access.CanSubmit(p, directory.Get(form)!));
            }
        });
        Test("Central dependencias sólo habilita su nivel 2 y rama nivel 3", () =>
        {
            var p = Fixtures.Profile("colaborador_dependencias");
            var scopes = access.Scopes(p, grants: [grant with { Origin = "central" }]);
            Check(scopes.Keys.Order().SequenceEqual(new[] { "A", "A3" }));
            Check(scopes.Values.All(s => s.Edit && !access.CanSubmit(p, s.Unit)));
            var nested = new UnitDirectory([.. Fixtures.Units, new("nested", "1001", "Otra principal", 2, "A", 2026),
                new("nested3", "1002", "Otra dependiente", 3, "nested", 2026)]);
            Check(new FormAccess(nested).Scopes(p, grants: [grant with { Origin = "central" }]).Keys.Order().SequenceEqual(new[] { "A", "A3" }));
        });
        Test("Rol efectivo sin origen central explícito no autoriza fallback", () =>
        {
            foreach (var origin in new[] { "aplicacion", "", "CENTRAL", "desconocido" })
                Check(access.Scopes(collaborator, grants: [grant with { Origin = origin }]).Count == 0);
            Check(access.Scopes(collaborator).Count == 0);
            Check(access.Scopes(collaborator, grants: [grant with { Origin = "central", RoleId = 999 }]).Count == 0);
        });
        Test("Central exige identidad individual, empleado, adscripción y módulo", () =>
        {
            foreach (var user in new[] { collaborator.User with { AccountType = "generica" }, collaborator.User with { Employee = null },
                collaborator.User with { UnitId = "110" }, collaborator.User with { Origin = "multiple" }, collaborator.User with { UnitId = "absent" } })
                Check(access.Scopes(collaborator with { User = user }, grants: [grant with { Origin = "central" }]).Count == 0);
            Check(access.Scopes(collaborator with { Modules = [] }, grants: [grant with { Origin = "central" }]).Count == 0);
        });
        Test("Central y delegada son independientes ante retiro local y central", () =>
        {
            var central = grant with { Id = 51, Origin = "central" };
            Check(access.Scopes(collaborator, [link with { Revoked = true }], [grant, central])["A3"].Edit);
            Check(access.Scopes(collaborator, [link with { Revoked = true }], [grant]).Count == 0);
            Check(access.Scopes(collaborator, [link], [grant])["A3"].Edit);
            Check(access.Scopes(collaborator, [link], [central])["A3"].Edit);
        });
        Test("Central conserva tipo N auxiliar, tipo 0 elegible y no usa prefijos", () =>
        {
            var units = new UnitDirectory([new("root", "099", "Principal", 2, null, 2026, Kind: "0"),
                new("aux", "6000", "Auxiliar", 3, "root", 2026, Kind: " n "),
                new("leaf", "01000", "Inferior", 4, "aux", 2026), new("other", "01001", "Ajena", 3, null, 2026)]);
            var scopes = new FormAccess(units).Scopes(collaborator with { User = collaborator.User with { UnitId = "leaf" } }, grants: [grant with { Origin = "central" }]);
            Check(scopes.Count == 1 && scopes["root"].Edit);
            var broken = new UnitDirectory([new("x", "06000", "Sin jerarquía", 4, "missing", 2026)]);
            Check(new FormAccess(broken).Scopes(collaborator with { User = collaborator.User with { UnitId = "x" } }, grants: [grant with { Origin = "central" }]).Count == 0);
        });
        Test("Central combinada no eleva administrador ni envío del supervisor ajeno", () =>
        {
            var admin = Fixtures.Profile("colaborador_dependencias", "administrador") with { User = collaborator.User with { Origin = "multiple" } };
            Check(access.Scopes(admin, grants: [grant with { Origin = "central" }]).Values.All(s => !s.Edit));
            var supervisor = Fixtures.Profile("colaborador_dependencias", "responsable_ur_supervisor") with { User = collaborator.User with { Employee = "9999" } };
            var scopes = access.Scopes(supervisor, grants: [grant with { Origin = "central" }]);
            Check(scopes["A3"].Edit && !scopes["B"].Edit && scopes.Values.All(s => !access.CanSubmit(supervisor, s.Unit)));
        });
        Test("Módulos desconocidos y rutas manipuladas denegados", () => { Check(!ModuleAccess.Allows(Fixtures.Profile("administrador"), "arbitrario")); Check(!ModuleAccess.Allows(Fixtures.Profile("administrador") with { Modules = [new(1, "procesos_operativos", "x", "/otra")] }, "procesos_operativos")); });
        Test("Configuración exige administrador además de módulo", () => Check(!ModuleAccess.Allows(Fixtures.Profile("responsable_ur"), "configuracion")));
        Test("Módulo hijo exige padre y vínculo exactos", () => { var p = Fixtures.Profile("administrador"); Check(ModuleAccess.Allows(p, "sincronizaciones")); Check(!ModuleAccess.Allows(p with { Modules = p.Modules.Where(m => m.Key != "configuracion").ToArray() }, "sincronizaciones")); Check(!ModuleAccess.Allows(p with { Modules = p.Modules.Select(m => m.Key == "sincronizaciones" ? m with { Parent = 99 } : m).ToArray() }, "sincronizaciones")); });
        Test("Formato inicial válido y avance cero", () => Check(FormSchema.Progress(schema.Validate(Blank(), Fixtures.Units[0])) == 0));
        Test("Encabezado histórico se conserva y preguntas son canónicas", () => { var data = Blank(); data["encabezado"]!["area"] = "forjada"; data["preguntas"]![0]!["pregunta"] = "forjada"; var valid = schema.Validate(data, Fixtures.Units[0]); Check(valid["encabezado"]!["area"]!.GetValue<string>() == "forjada" && valid["preguntas"]![0]!["pregunta"]!.GetValue<string>() != "forjada"); });
        Reject("No eliminar preguntas fijas", d => d["preguntas"]!.AsArray().RemoveAt(0));
        Reject("No inventar opción de respuesta", d => d["preguntas"]![0]!["respuesta"] = "inventada");
        Reject("No repetir ID de fila", d => d["identificacion"]!.AsArray().Add(d["identificacion"]![0]!.DeepClone()));
        Reject("No repetir código de proceso", d => { d["identificacion"]![0]!["codigo"] = "PO-01"; var row = d["identificacion"]![0]!.DeepClone(); row["id"] = "otro"; d["identificacion"]!.AsArray().Add(row); });
        Reject("Código debe coincidir completamente", d => d["identificacion"]![0]!["codigo"] = "PO-01\n");
        Reject("No aceptar prioridad cero", d => d["identificacion"]![0]!["prioridad"] = "0");
        Reject("Proceso ajeno denegado", d => d["sistemas"]![0]!["proceso"] = "PO-99");
        Reject("Fecha imposible denegada", d => d["acuerdos"]![0]!["fecha"] = "2026-02-30");
        Reject("Campo adicional en fila denegado", d => d["datos"]![0]!["extra"] = "x");
        Reject("Texto sobre límite denegado", d => d["datos"]![0]!["dato"] = new string('x', 4001));
        Reject("Campos requeridos no se omiten", d => d["acuerdos"]![0]!.AsObject().Remove("fecha"));
        Reject("Máximo 200 filas", d => { var rows = d["acuerdos"]!.AsArray(); for (var i = 1; i <= 200; i++) { var row = rows[0]!.DeepClone(); row["id"] = i.ToString(); rows.Add(row); } });
        Test("Evaluaciones vacías heredadas se normalizan", () => { var data = Blank(); data["evaluaciones"] = new JsonArray(); Check(schema.Validate(data, Fixtures.Units[0])["evaluaciones"] is JsonObject); });
        Test("Porcentaje cliente no se usa", () => { var data = Blank(); data["porcentaje"] = 100; Check(FormSchema.Progress(schema.Validate(data, Fixtures.Units[0])) == 0); });
        Test("Todos los campos contabilizados llegan a 100", () =>
        {
            var data = Blank();
            foreach (var section in new[] { "identificacion", "sistemas", "datos", "acuerdos" }) foreach (var (key, _) in data[section]![0]!.AsObject().ToArray()) if (key != "id") data[section]![0]![key] = "Texto";
            data["identificacion"]![0]!["codigo"] = "PO-01"; data["identificacion"]![0]!["prioridad"] = "1"; data["identificacion"]![0]!["validacion"] = "V";
            data["sistemas"]![0]!["proceso"] = "PO-01"; data["sistemas"]![0]!["estado"] = "Funciona";
            data["datos"]![0]!["proceso"] = "PO-01"; data["datos"]![0]!["origen"] = "Se origina en este proceso"; data["acuerdos"]![0]!["fecha"] = "2026-09-30";
            foreach (var q in data["preguntas"]!.AsArray()) q!["respuesta"] = q["opciones"] is JsonArray choices ? choices[0]!.DeepClone() : JsonValue.Create("Respuesta");
            data["evaluaciones"]!["PO-01"] = new JsonArray(Enumerable.Range(0, 9).Select(_ => (JsonNode)new JsonObject { ["criterio"] = "alterado", ["valor"] = "5", ["obs"] = "" }).ToArray());
            var valid = schema.Validate(data, Fixtures.Units[0]); Check(FormSchema.Progress(valid) == 100); FormSchema.RequireComplete(valid); Check(valid["encabezado"]!["fecha"]!.ToString() == ""); Check(valid["evaluaciones"]!["PO-01"]![0]!["criterio"]!.GetValue<string>() != "alterado");
        });

        Http("Portada pública no consulta Nexo y conserva tipografía", async (app, client) => { app.Nexo.Outage = true; var r = await client.GetAsync("/"); Check(r.IsSuccessStatusCode); var html = await r.Content.ReadAsStringAsync(); Check(html.Contains("Space+Grotesk") && html.Contains("IBM+Plex+Sans") && html.Contains("Registrar un proceso operativo")); });
        Http("ASP.NET sirve enlace profundo y bundle React generado", async (_, client) =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/formatos/A"); request.Headers.Accept.ParseAdd("text/html");
            var response = await client.SendAsync(request); Check(response.IsSuccessStatusCode, "Ejecuta npm run build antes de verificar el host de React.");
            var html = await response.Content.ReadAsStringAsync();
            var asset = System.Text.RegularExpressions.Regex.Match(html, "src=\"(/assets/[^\"]+)\"");
            Check(asset.Success && html.Contains("id=\"app\""));
            Check((await client.GetAsync(asset.Groups[1].Value)).IsSuccessStatusCode);
        });
        Http("Anónimo y headers de rol falsos no acceden", async (app, _) => { using var client = app.Client(false); client.DefaultRequestHeaders.Add("X-Role", "administrador"); Check((await client.GetAsync("/inicio?email=persona@example.test")).StatusCode == HttpStatusCode.Unauthorized); });
        Http("Módulo se revalida al siguiente request", async (app, client) => { Check((await client.GetAsync("/inicio")).IsSuccessStatusCode); app.Nexo.Current = app.Nexo.Current with { Modules = [] }; Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden); });
        Http("Nexo caído no filtra secretos ni autoriza", async (app, client) => { app.Nexo.Outage = true; var r = await client.GetAsync("/inicio"); Check(r.StatusCode == HttpStatusCode.ServiceUnavailable && !(await r.Content.ReadAsStringAsync()).Contains("SYNTHETIC_SECRET")); });
        Http("Falla SQL no filtra secretos", async (app, client) => { app.Store.Outage = true; var r = await client.GetAsync("/inicio"); Check(r.StatusCode == HttpStatusCode.ServiceUnavailable && !(await r.Content.ReadAsStringAsync()).Contains("SYNTHETIC_DATABASE")); });
        Http("Página no expone empleado ni tokens centrales", async (_, client) => { var r = await client.GetAsync("/inicio"); var raw = await r.Content.ReadAsStringAsync(); Check(r.IsSuccessStatusCode && !raw.Contains("0001") && !raw.Contains("num_empleado") && !raw.Contains("representacion_token")); Check(r.Headers.CacheControl?.NoStore == true); });
        Http("GET formato es lectura sin crear registro", async (app, client) => { Check((await client.GetAsync("/formatos/A")).IsSuccessStatusCode); Check(await app.Store.Get("A", default) is null); });
        Http("UR fuera de alcance denegada", async (_, client) => Check((await client.GetAsync("/formatos/B")).StatusCode == HttpStatusCode.Forbidden));
        Http("CSRF ausente y falso denegados", async (_, client) => { Check((int)(await client.PutAsJsonAsync("/formatos/A", new SaveForm(0, Blank()))).StatusCode == 419); client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "forjado"); Check((int)(await client.PutAsJsonAsync("/formatos/A", new SaveForm(0, Blank()))).StatusCode == 419); });
        Http("PUT completo retirado no evita las reservas por bloque", async (app, client) => { await Csrf(client); var r = await client.PutAsJsonAsync("/formatos/A", new SaveForm(0, Blank())); Check((int)r.StatusCode == 428); Check(await app.Store.Get("A", default) is null); });
        Http("Pestaña con contexto obsoleto no guarda", async (app, client) => { await Csrf(client); client.DefaultRequestHeaders.Add("X-TDV2-Context", "representacion-anterior"); Check((await client.PutAsJsonAsync("/formatos/A", new SaveForm(0, Blank()))).StatusCode == HttpStatusCode.Conflict); Check(await app.Store.Get("A", default) is null); });
        Http("Revocación de rol impide nuevo guardado", async (app, client) => { await Csrf(client); app.Nexo.Current = app.Nexo.Current with { Roles = [] }; Check((await client.PutAsJsonAsync("/formatos/A", new SaveForm(0, Blank()))).StatusCode == HttpStatusCode.Forbidden); });
        Http("Logout requiere CSRF y funciona con Nexo caído", async (app, client) => { app.Nexo.Outage = true; Check((int)(await client.PostAsJsonAsync("/logout", new { })).StatusCode == 419); await Csrf(client); Check((await client.PostAsJsonAsync("/logout", new { })).IsSuccessStatusCode); });
        Http("Bloques pendientes exigen permiso antes de responder", async (app, client) => { using var guest = app.Client(false); Check((await guest.GetAsync("/connect")).StatusCode == HttpStatusCode.ServiceUnavailable); Check((await client.GetAsync("/configuracion/sincronizaciones")).StatusCode == HttpStatusCode.Forbidden); });

        var failures = 0;
        foreach (var test in Cases)
        {
            try { await test.Run(); Console.WriteLine("PASS " + test.Name); }
            catch (Exception error) { failures++; Console.WriteLine("FAIL " + test.Name + ": " + error.Message); }
        }
        Console.WriteLine($"TOTAL {Cases.Count}; PASS {Cases.Count - failures}; FAIL {failures}");
        return failures == 0 ? 0 : 1;
    }
}
