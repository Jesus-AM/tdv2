using System.Net;
using System.Net.Http.Json;
using Tdv2.Services;

namespace Tdv2.NativeVerification;
internal static partial class NativeTests
{
    private static void RegisterPhotoCases(NativeDatabase database, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        static string Url(string email = "persona@uacj.mx", string unit = "A") => $"/formatos/{unit}/participantes/{ParticipantIdentity.Key(email)}/foto";
        Test("Foto/Graph debe acreditar la identidad de la miniatura y conserva renovación ante 401", async (app, client) =>
        {
            await Login(app, client); app.Microsoft.PhotoAvailable = true;
            app.Microsoft.MeObjectIdOverride = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is null);
            app.Microsoft.MeObjectIdOverride = null; app.Clock.Offset = TimeSpan.FromMinutes(3); app.Microsoft.PhotoFailure = true;
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is null && app.Microsoft.Refreshes == 1);
            app.Microsoft.PhotoFailure = false; app.Clock.Offset = TimeSpan.FromMinutes(6);
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is not null);
        });
        Test("Foto/reserva verificada, caché compartida, renovación y rechazo tras liberar", async (app, client) =>
        {
            app.Microsoft.PhotoAvailable = true;
            await Login(app, client); await Csrf(client); var tab = Guid.NewGuid();
            Check((await client.GetAsync(Url())).StatusCode == HttpStatusCode.NotFound);
            await Lease(client, tab, "medios");
            var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => client.GetAsync(Url())));
            Check(responses.All(r => r.IsSuccessStatusCode) && app.Microsoft.PhotoRequests == 1);
            Check((await Json(responses[0]))["photo"]!.ToString().StartsWith("data:image/png;base64,"));
            Check(responses[0].Headers.CacheControl?.NoStore == true);
            Check((await client.GetAsync("/user/photo")).IsSuccessStatusCode && app.Microsoft.PhotoRequests == 1);
            app.Clock.Offset = TimeSpan.FromMinutes(16);
            Check((await client.GetAsync(Url())).IsSuccessStatusCode && app.Microsoft.PhotoRequests == 2);
            await database.Sql("UPDATE formato_bloques SET reserva_id=NULL");
            Check((await client.GetAsync(Url())).StatusCode == HttpStatusCode.NotFound && app.Microsoft.PhotoRequests == 2);
        });
        Test("Foto/no es directorio: UR ajena, participante ausente, sesión revocada y acceso suspendido", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await Lease(client, Guid.NewGuid(), "medios");
            Check((await client.GetAsync(Url(unit: "B"))).StatusCode == HttpStatusCode.Forbidden);
            Check((await client.GetAsync(Url("ajena@uacj.mx"))).StatusCode == HttpStatusCode.NotFound);
            await database.Sql("DELETE FROM fixture_roles WHERE email='persona@uacj.mx'");
            Check(!(await client.GetAsync(Url())).IsSuccessStatusCode && app.Microsoft.PhotoRequests == 0);
            await database.Sql("DELETE FROM tdv2_sessions");
            Check((await client.GetAsync(Url())).StatusCode == HttpStatusCode.Unauthorized);
        });
        Test("Foto/iniciales ante Graph ausente, identidad no coincidente y vencimiento de reserva", async (app, client) =>
        {
            await Login(app, client); await Csrf(client); await Lease(client, Guid.NewGuid(), "medios");
            Check((await Json(await client.GetAsync(Url())))["photo"] is null && app.Microsoft.PhotoRequests == 1);
            app.Clock.Offset = TimeSpan.FromMinutes(3); app.Microsoft.PhotoAvailable = true;
            // La cookie del solicitante sigue intacta; el vínculo solicitado debe acreditar otra cuenta real.
            await database.Sql("UPDATE formato_bloques SET participante='" + ParticipantIdentity.Key("ajena@uacj.mx") + "'");
            Check((await client.GetAsync(Url("ajena@uacj.mx"))).StatusCode == HttpStatusCode.NotFound);
            await database.Sql("UPDATE formato_bloques SET participante='" + ParticipantIdentity.Key("persona@uacj.mx") + "',vence_en=now()-interval '1 second'");
            Check((await client.GetAsync(Url())).StatusCode == HttpStatusCode.NotFound && app.Microsoft.PhotoRequests == 1);
        });
        Test("Foto/representación usa objetivo verificado o iniciales; nunca la foto del administrador", async (app, client) =>
        {
            await SetupAccess(database); app.Microsoft.PhotoAvailable = true;
            await Login(app, client); await Csrf(client);
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is not null);
            Check((await StartRepresentation(client)).IsSuccessStatusCode); await Context(client);
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is null && app.Microsoft.PhotoRequests == 1);
            using var target = app.Client(); app.Microsoft.Email = app.Microsoft.Mail = "encargada@uacj.mx";
            app.Microsoft.Id = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee"; await Login(app, target); await Csrf(target);
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is not null && app.Microsoft.PhotoRequests == 2);
            await Lease(client, Guid.NewGuid(), "medios");
            var live = await Live(target, Guid.NewGuid());
            var lease = live["bloques"]!.AsArray().Single(b => b!["key"]!.ToString() == "medios")!["reserva"]!;
            Check(lease["foto"]!.ToString() == Url("encargada@uacj.mx"));
            Check((await target.GetAsync(Url("encargada@uacj.mx"))).IsSuccessStatusCode && app.Microsoft.PhotoRequests == 2);
            Check((await target.GetAsync(Url())).StatusCode == HttpStatusCode.NotFound);
            await database.Sql("UPDATE fixture_representation SET revoked=true");
            Check(!(await client.GetAsync(Url("encargada@uacj.mx"))).IsSuccessStatusCode);
        });
    }
}
