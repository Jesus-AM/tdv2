using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tdv2.Competencias;
using Tdv2.Domain;
using Tdv2.Domain.Entities;
using Tdv2.Infrastructure;
using Tdv2.Integrations.Microsoft;
using Tdv2.Security;

namespace Tdv2.Verification;

// Revisión de modelo y acceso: no abre conexiones, aplica migraciones ni ejecuta SQL.
internal static class IdentityLinkReviewTests
{
    internal static async Task Run(string destination)
    {
        var checks = new List<string>();
        void Check(bool value, string message) { if (!value) throw new Exception(message); checks.Add(message); }
        static ClaimsPrincipal Actor() => new(new ClaimsIdentity([
            new(ClaimTypes.NameIdentifier, "synthetic-id"), new(ClaimTypes.Email, "persona@example.test"),
            new(NexoActorIdentity.Claim, "20")], "Synthetic"));

        // Inspeccionar metadatos no inicializa una base. No hay cadena de conexión.
        using (var db = new Tdv2DbContext(new DbContextOptionsBuilder<Tdv2DbContext>().UseNpgsql().Options))
        {
            var user = db.Model.FindEntityType(typeof(User))!;
            Check(user.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { nameof(User.MicrosoftTenantId), nameof(User.MicrosoftId) })),
                "users ya impide duplicar el par Microsoft; no necesita otro índice para ello");
            Check(!user.GetProperties().Any(p => p.Name.Contains("Nexo", StringComparison.OrdinalIgnoreCase))
                && db.Model.GetEntityTypes().Where(e => e.ClrType != typeof(InstitutionalIdentityLink))
                    .SelectMany(e => e.GetProperties()).All(p => p.Name != nameof(InstitutionalIdentityLink.NexoUserId)),
                "el modelo previo no tiene una referencia persistente al usuario canónico Nexo");
            var link = db.Model.FindEntityType(typeof(InstitutionalIdentityLink))!;
            Check(link.FindPrimaryKey()!.Properties.Select(p => p.Name).SequenceEqual(new[] { "TenantId", "ObjectId" })
                && link.GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == "NexoUserId"),
                "la propuesta restringe una correspondencia canónica por objeto y por usuario Nexo");
        }
        // UpOperations/DownOperations son descripciones EF: no ejecutan la migración.
        var migration = new Tdv2.Migrations.VerifiedInstitutionalIdentityLinks();
        var created = migration.UpOperations.OfType<CreateTableOperation>().Single();
        Check(created.Name == "tdv2_identity_links" && created.Schema == "public"
            && created.Columns.Select(c => c.Name).Order().SequenceEqual(new[] { "microsoft_tenant_id", "microsoft_id", "nexo_user_id", "verified_at", "verified_by", "verification_reference" }.Order())
            && created.ForeignKeys.Count == 0 && created.CheckConstraints.Count == 1
            && migration.UpOperations.Count == 3 && migration.UpOperations.OfType<CreateIndexOperation>().Single().IsUnique
            && migration.UpOperations.OfType<SqlOperation>().Single().Sql == "REVOKE ALL ON TABLE public.tdv2_identity_links FROM PUBLIC;"
            && migration.DownOperations.Single() is DropTableOperation { Name: "tdv2_identity_links", Schema: "public" },
            "la migración sólo describe una tabla, su índice único y REVOKE; no modifica usuarios ni encuestas");

        using (var profile = JsonDocument.Parse("""
            {"id":"bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb","mail":"trabajo@uacj.mx",
             "userPrincipalName":"inicio@uacj.mx","proxyAddresses":["SMTP:trabajo@uacj.mx","smtp:institucional@uacj.mx","SIP:chat@uacj.mx"]}
            """))
        {
            var person = MicrosoftPerson.Parse(profile.RootElement);
            Check(person.VerifiedAddresses.Contains("institucional@uacj.mx") && person.VerifiedAddresses.Contains("inicio@uacj.mx")
                && !person.VerifiedAddresses.Contains("chat@uacj.mx"),
                "el reconocimiento UPN/SMTP funciona sin tabla ni acceso a bases; SIP no acredita alias");
        }

        foreach (var (td, digital) in new[] { (true, false), (false, true), (true, true), (false, false) })
        {
            var tdSource = new Source(false, td); var digitalSource = new Source(true, digital);
            await using var basis = new Application();
            await using var app = basis.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<INexoProfiles>(); services.AddSingleton<INexoProfiles>(tdSource);
                services.RemoveAll<IDigitalNexo>(); services.AddSingleton<IDigitalNexo>(digitalSource);
            }));
            using var client = app.CreateClient(new() { BaseAddress = new("https://localhost"), AllowAutoRedirect = false });
            client.DefaultRequestHeaders.Add("X-Synthetic-Session", "yes");
            var response = await client.GetAsync("/accesos"); response.EnsureSuccessStatusCode();
            var result = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            var expected = new[] { td ? "/inicio" : null, digital ? DigitalRoutes.Base : null }.OfType<string>().ToArray();
            var destinations = result["redirect"] is { } direct ? new[] { direct.ToString() }
                : result["props"]!["available"]!["options"]!.AsArray().Select(o => o!["path"]!.ToString()).ToArray();
            Check(destinations.SequenceEqual(expected) && tdSource.Requested.All(id => id == 20) && digitalSource.Requested.All(id => id == 20)
                && tdSource.EmailReads == 0 && digitalSource.EmailReads == 0,
                $"HTTP Mi espacio: TD={td}, Competencias={digital}; un mismo ID canónico y permisos separados");
            if (td) Check((await client.GetAsync("/inicio")).IsSuccessStatusCode, "Procedimientos abre con su autorización vigente");
            else Check((await client.GetAsync("/inicio")).StatusCode == HttpStatusCode.Forbidden, "Procedimientos deniega sin buscar otro alias");

            // Servicios productivos de Competencias, sin abrir la persistencia de la encuesta.
            using var scope = app.Services.CreateScope();
            var http = new DefaultHttpContext { User = Actor(), RequestServices = scope.ServiceProvider };
            http.Request.Method = "GET"; http.Request.Path = DigitalRoutes.Base + "/encuesta";
            scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = http;
            try
            {
                var access = await scope.ServiceProvider.GetRequiredService<DigitalAccess>().Require(http, 2);
                Check(digital && access.Profile.NexoUserId == 20 && access.MenuPages.SequenceEqual(new[] { 2, 3 }),
                    "Competencias permite encuesta/informe al mismo usuario canónico");
            }
            catch (DomainProblem e) when (e.Status == 403)
            { Check(!digital, "Competencias deniega independientemente del permiso de Procedimientos"); }
        }

        foreach (var digital in new[] { false, true })
        {
            var site = digital ? "Competencias" : "Procedimientos";
            var source = new Source(digital, true);
            var actor = Actor(); var first = await source.ForActor(actor, default);
            // Mismo correo pero otro usuario central, tras una reasignación: el estado
            // local previo (id, tid, oid, email) no registra que antes se acreditó el 20.
            source.Canonical = 21;
            var byEmail = await source.ForEmail(first.User.Email, default);
            var denied = false;
            try { await source.ForActor(actor, default); } catch (DomainProblem e) when (e.Status == 403) { denied = true; }
            Check(byEmail.NexoUserId == 21 && denied, site + ": correo idéntico no permite cambiar el usuario canónico de 20 a 21");
            source.Canonical = 20; source.Allowed = false;
            denied = false;
            try { await source.ForActor(actor, default); } catch (DomainProblem e) when (e.Status == 403) { denied = true; }
            Check(denied, site + ": conservar el vínculo no conserva permisos revocados");
            source.TemporarilyUnavailable = true;
            var unavailable = false;
            try { await source.ForActor(actor, default); } catch (DomainProblem e) when (e.Status == 503) { unavailable = true; }
            Check(unavailable, site + ": fallo de fuente se distingue de denegación");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination))!);
        await File.WriteAllTextAsync(destination, JsonSerializer.Serialize(new {
            utc = DateTimeOffset.UtcNow, database = false, migrationsApplied = false,
            transport = "ASP.NET TestServer; identidad, Nexo y catálogos sintéticos en memoria",
            passed = checks.Count, checks
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"PASS {checks.Count} comprobaciones de modelo/alias/acceso; cero conexiones y migraciones.");
    }

    private sealed class Source(bool digital, bool allowed) : IDigitalNexo
    {
        internal bool Allowed = allowed;
        internal long Canonical = 20;
        internal bool TemporarilyUnavailable;
        internal List<long> Requested = [];
        internal int EmailReads;
        private Profile Profile() => new(new("persona@example.test", "Persona sintética", "0001", "A4"),
            [new(1, digital ? "participante" : "responsable_ur", "Rol sintético")],
            digital ? [new(2, "encuesta", "Encuesta", DigitalRoutes.Base + "/encuesta"), new(3, "informe", "Mi informe y ruta", DigitalRoutes.Base + "/mi-informe-ruta")]
                : [new(1, "procesos_operativos", "Procedimientos", "/inicio")], Canonical);
        public Task<Profile> ForUser(long id, CancellationToken ct)
        {
            Requested.Add(id);
            if (TemporarilyUnavailable) throw new DomainProblem(503, "Fuente sintética no disponible.");
            if (!Allowed || id != Canonical) throw new DomainProblem(403, "Sin acceso sintético.");
            return Task.FromResult(Profile());
        }
        public Task<Profile> ForEmail(string email, CancellationToken ct) { EmailReads++; return Task.FromResult(Profile()); }
        public Task<IReadOnlyList<Grant>> Grants(string email, CancellationToken ct) => Task.FromResult<IReadOnlyList<Grant>>([]);
        public Task<JsonNode> Represent(string action, string actor, JsonObject? data, CancellationToken ct) => throw new InvalidOperationException("Esta revisión no representa usuarios.");
    }
}
