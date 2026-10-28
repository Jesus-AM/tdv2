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
    public static async Task<int> Main(string[] args)
    {
        if (args is ["--verify-digital-edition", var editionDestination])
        {
            DigitalEditionTests.Run(editionDestination);
            return 0;
        }
        if (args is ["--verify-digital-presentation", var digitalDestination])
        {
            DigitalPresentationTests.Run(digitalDestination);
            return 0;
        }
        if (args is ["--verify-public-assets", var assetsDestination])
        {
            await PublicAssetsTests.Run(assetsDestination);
            return 0;
        }
        // Exportación visual anónima del Razor real: dobles en memoria, sin PostgreSQL ni migraciones.
        if (args is ["--export-public", var destination])
        {
            await using var app = new Application();
            using var client = app.Client(authenticated: false);
            var response = await client.GetAsync("/");
            response.EnsureSuccessStatusCode();
            await File.WriteAllTextAsync(destination, await response.Content.ReadAsStringAsync());
            return 0;
        }
        var directory = new UnitDirectory(Fixtures.Units);
        var access = new FormAccess(directory);
        var path = Path.Combine(AppContext.BaseDirectory, "Contracts", "procesos_operativos.json");
        var schema = new FormSchema(File.ReadAllText(path));
        SystemCatalogTests.Register(Cases, schema);
        ApplicationSelectionTests.Register(Cases, schema);
        Http("Catálogos de aplicaciones sólo aceptan catálogos conocidos y el alcance autorizado", async (app, client) =>
        {
            await Csrf(client);
            foreach (var kind in new[] { "moviles", "web" })
            {
                var response = await client.GetAsync("/formatos/A/aplicaciones/" + kind);
                Check(response.IsSuccessStatusCode);
                var catalog = await Json(response); Check(catalog["estado"]!.ToString() == "pendiente" && catalog["aplicaciones"]!.AsArray().Count == 0);
                Check((await client.GetAsync("/formatos/B/aplicaciones/" + kind)).StatusCode == HttpStatusCode.Forbidden);
            }
            Check((await client.GetAsync("/formatos/A/aplicaciones/ajeno")).StatusCode == HttpStatusCode.NotFound);
            app.Nexo.Current = Fixtures.Profile();
            app.Nexo.Outage = true;
            Check(!(await client.GetAsync("/formatos/A/aplicaciones/moviles")).IsSuccessStatusCode);
        });
        CaptureProgressTests.Register(Cases, schema);
        CollaborationVisibilityTests.Register(Cases);
        Http("Colaboradores GET filtra asignaciones centrales/delegadas por rama; visibilidad no concede retiro", async (app, client) =>
        {
            app.Nexo.Current = Fixtures.Profile("administrador", "responsable_ur");
            var local = CollaborationVisibilityTests.Link(1, grantor: "A3");
            var branch = CollaborationVisibilityTests.Link(2, "dependencias", "A", "A");
            app.Collaborators.Links.AddRange([local, branch]);
            app.Collaborators.People.Add(CollaborationVisibilityTests.Identity(CollaborationVisibilityTests.Grant(local), CollaborationVisibilityTests.Grant(branch), new(100, 30, "B", "central")));
            var alien = CollaborationVisibilityTests.Person("B3") with { User = CollaborationVisibilityTests.Person("B3").User with { Email = "ajeno@uacj.mx" } };
            app.Collaborators.People.Add(new(alien, [new(101, 30, "B3", "central")]));
            var response = await client.GetAsync("/colaboradores?ur=B"); Check(response.IsSuccessStatusCode);
            var props = (await Json(response))["props"]!; var rows = props["colaboraciones"]!.AsArray();
            Check(rows.Count == 3 && rows.All(r => r!["email"]!.ToString() != "ajeno@uacj.mx"));
            Check(!app.Collaborators.Affiliations.Contains("B3") && app.Collaborators.Affiliations.Contains("A4"));
            Check(rows.Single(r => r!["origen"]!.ToString() == "central")!["puedeRetirar"]!.GetValue<bool>() == false);
            Check(!rows.Single(r => r!["id"]?.ToString() == "1")!["puedeRetirar"]!.GetValue<bool>() && rows.Single(r => r!["id"]?.ToString() == "2")!["puedeRetirar"]!.GetValue<bool>());
            app.Nexo.Current = Fixtures.Profile("responsable_ur") with { User = Fixtures.Profile().User with { Employee = "0002", UnitId = "A3" } };
            rows = (await Json(await client.GetAsync("/colaboradores")))["props"]!["colaboraciones"]!.AsArray();
            Check(rows.Count == 2 && rows.All(r => r!["tipo"]!.ToString() == "local") && rows.Single(r => r!["id"]?.ToString() == "1")!["puedeRetirar"]!.GetValue<bool>());
        });
        Http("Colaboradores suspensión/fallo de publicación no conserva una lista efectiva obsoleta", async (app, client) =>
        {
            var link = CollaborationVisibilityTests.Link(5); app.Collaborators.Links.Add(link);
            app.Collaborators.People.Add(CollaborationVisibilityTests.Identity(CollaborationVisibilityTests.Grant(link)));
            Check((await Json(await client.GetAsync("/colaboradores")))["props"]!["colaboraciones"]!.AsArray().Count == 1);
            app.Collaborators.People.Clear(); Check((await Json(await client.GetAsync("/colaboradores")))["props"]!["colaboraciones"]!.AsArray().Count == 0);
            app.Collaborators.Outage = true; Check((await client.GetAsync("/colaboradores")).StatusCode == HttpStatusCode.ServiceUnavailable);
        });
        Http("Colaboradores responsable sin facultad de delegar consulta su asignación sin adquirir alta ni retiro", async (app, client) =>
        {
            app.Nexo.Current = Fixtures.Profile("responsable_ur") with { User = Fixtures.Profile().User with { Employee = "0002", UnitId = "A3" } };
            app.Collaborators.DelegationEnabled = false;
            var link = CollaborationVisibilityTests.Link(7, grantor: "A3"); app.Collaborators.Links.Add(link);
            app.Collaborators.People.Add(CollaborationVisibilityTests.Identity(CollaborationVisibilityTests.Grant(link)));
            var props = (await Json(await client.GetAsync("/colaboradores")))["props"]!;
            Check(props["unidades"]!.AsArray().Count == 0 && props["colaboraciones"]!.AsArray().Count == 1
                && !props["colaboraciones"]![0]!["puedeRetirar"]!.GetValue<bool>());
            await Csrf(client);
            Check((await client.DeleteAsync("/colaboradores/7")).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.PostAsJsonAsync("/colaboradores", new { ur = "A3", email = "colabora@uacj.mx", id_ur = "A4", tipo = "local" })).StatusCode == HttpStatusCode.Forbidden);
        });
        Http("Colaboradores consulta sin responsabilidad y mutaciones sin autorización se rechazan antes de leer asignaciones", async (app, client) =>
        {
            app.Nexo.Current = Fixtures.Profile("consulta_institucional");
            Check((await client.GetAsync("/colaboradores")).StatusCode == HttpStatusCode.Forbidden && app.Collaborators.Reads == 0);
            await Csrf(client); Check((await client.DeleteAsync("/colaboradores/1")).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.PostAsJsonAsync("/colaboradores", new { ur = "A", email = "colabora@uacj.mx", id_ur = "A4", tipo = "local" })).StatusCode == HttpStatusCode.Forbidden);
        });
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
            data["identificacion"]![0]!["usuario"] = new JsonArray("Docentes");
            data["sistemas"]![0]!["proceso"] = "PO-01"; data["sistemas"]![0]!["estado"] = "bien";
            data["sistemas"]![0]!["sistema"] = "limesurvey"; data["sistemas"]![0]!["uso"] = "consultar";
            data["datos"]![0]!["proceso"] = "PO-01"; data["datos"]![0]!["origen"] = "Se origina en este proceso"; data["acuerdos"]![0]!["fecha"] = "2026-09-30";
            foreach (var q in data["preguntas"]!.AsArray()) q!["respuesta"] = q["opciones"] is JsonArray choices ? choices[0]!.DeepClone() : JsonValue.Create("Respuesta");
            data["evaluaciones"]!["PO-01"] = new JsonArray(Enumerable.Range(0, 9).Select(_ => (JsonNode)new JsonObject { ["criterio"] = "alterado", ["valor"] = "5", ["obs"] = "" }).ToArray());
            var valid = schema.Validate(data, Fixtures.Units[0]); Check(FormSchema.Progress(valid) == 100); FormSchema.RequireComplete(valid); Check(valid["encabezado"]!["fecha"]!.ToString() == ""); Check(valid["evaluaciones"]!["PO-01"]![0]!["criterio"]!.GetValue<string>() != "alterado");
        });

        Http("Portada pública no consulta Nexo y carga Noto Sans local, identidad compartida y el quiz", async (app, client) =>
        {
            app.Nexo.Outage = true;
            var r = await client.GetAsync("/"); Check(r.IsSuccessStatusCode);
            var html = await r.Content.ReadAsStringAsync();
            Check(html.Contains("/css/institutional.css") && html.Contains("/fonts/noto-sans/NotoSans-Regular.ttf") && !html.Contains("fonts.googleapis.com")
                && !html.Contains("/fonts/roboto/") && html.Contains("Registrar un procedimiento institucional")
                && html.Contains("Jugar Ahora") && html.Contains("¿Qué tanto sabes de Microsoft 365?")
                && !html.Contains("Pon a prueba tus conocimientos de Microsoft 365."));
        });
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
        Http("Avance confirmado coincide en Inicio, formato y revisión tras recargar; GET no reescribe ni concede edición", async (app, client) =>
        {
            client.DefaultRequestHeaders.Add("X-TDV2-Page", "1");
            var content = Blank(); content["identificacion"]![0]!["tramite"] = "Parcial";
            app.Store.Seed("A", new StoredForm("A", content, 1, 100, DateTimeOffset.UtcNow, "persona@example.test"));
            async Task Verify(int expected, bool ready)
            {
                var index = (await Json(await client.GetAsync("/inicio")))["props"]!;
                var form = (await Json(await client.GetAsync("/formatos/A")))["props"]!;
                Check(index["formatos"]!.AsArray().Single(f => f!["id_ur"]!.ToString() == "A")!["porcentaje"]!.GetValue<int>() == expected);
                Check(form["porcentaje"]!.GetValue<int>() == expected && form["porcentajeEtapa"]!.GetValue<int>() == expected);
                Check(form["entrega"]!["revision"]!["listo"]!.GetValue<bool>() == ready);
            }
            await Verify(14, false); Check((await app.Store.Get("A", default))!.Progress == 100); // Cache anterior intacta en GET.
            var row = content["identificacion"]![0]!; row["codigo"] = "PO-01"; row["prioridad"] = "3"; row["validacion"] = "V";
            row["usuario"] = new JsonArray("Egresados"); row["resultado"] = "Constancia"; row["responsable"] = "Área";
            var tool = content["sistemas"]![0]!; tool["proceso"] = "PO-01"; tool["sistema"] = "otra"; tool["uso"] = "consultar"; tool["estado"] = "bien";
            await app.Store.Save("A", 1, schema.Validate(content, Fixtures.Units[0]), "persona@example.test", default); await Verify(91, false);
            tool["sistemaOtro"] = "Propia"; await app.Store.Save("A", 2, schema.Validate(content, Fixtures.Units[0]), "persona@example.test", default); await Verify(100, true);
            app.Nexo.Current = Fixtures.Profile("consulta_institucional"); await Verify(100, true);
            var readOnly = (await Json(await client.GetAsync("/formatos/A")))["props"]!;
            Check(!readOnly["editable"]!.GetValue<bool>() && !readOnly["puedeEnviar"]!.GetValue<bool>());
            Check(readOnly["contenido"]!["identificacion"]![0]!["usuario"]![0]!.ToString() == "Egresados");
        });
        Http("Catálogo de módulos exige sesión, UR y módulo vigentes; sin catálogo no crea formato", async (app, client) =>
        {
            Check((await Json(await client.GetAsync("/formatos/A/modulos-sii")))["estado"]!.ToString() == "pendiente");
            Check(await app.Store.Get("A", default) is null);
            Check((await client.GetAsync("/formatos/B/modulos-sii")).StatusCode == HttpStatusCode.Forbidden);
            using var guest = app.Client(false); Check((await guest.GetAsync("/formatos/A/modulos-sii")).StatusCode == HttpStatusCode.Unauthorized);
            app.Nexo.Current = app.Nexo.Current with { Modules = [] };
            Check((await client.GetAsync("/formatos/A/modulos-sii")).StatusCode == HttpStatusCode.Forbidden);
        });
        Http("Avance histórico enviado conserva porcentaje, contenido, fecha y bloqueo sin reinterpretar el envío", async (app, client) =>
        {
            client.DefaultRequestHeaders.Add("X-TDV2-Page", "1");
            var content = Blank(); content["identificacion"]![0]!["validacion"] = "N";
            var sent = DateTimeOffset.Parse("2025-09-10T12:00:00Z");
            app.Store.Seed("A", new StoredForm("A", content, 3, 37, sent, "persona@example.test", SubmittedAt: sent, Year: 2025));
            var page = (await Json(await client.GetAsync("/formatos/A")))["props"]!;
            Check(page["porcentaje"]!.GetValue<int>() == 37 && !page["editable"]!.GetValue<bool>() && !page["puedeEnviar"]!.GetValue<bool>());
            Check(page["entrega"]!["etapa"]!["estado"]!.ToString() == "historico");
            var stored = await app.Store.Get("A", default);
            Check(stored!.SubmittedAt == sent && stored.Version == 3 && JsonNode.DeepEquals(stored.Content, content));
        });
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
