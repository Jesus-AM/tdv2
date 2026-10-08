using System.Net;
using System.Net.Http.Json;
using Tdv2.Services;
using Microsoft.AspNetCore.DataProtection;
using Tdv2.Security;

namespace Tdv2.NativeVerification;
internal static partial class NativeTests
{
    private static void RegisterPhotoCases(NativeDatabase database, Action<string, Func<NativeApplication, HttpClient, Task>> Test)
    {
        static string Url(string email = "persona@uacj.mx", string unit = "A") => $"/formatos/{unit}/participantes/{ParticipantIdentity.Key(email)}/foto";
        Test("Foto/persistencia entre páginas y recarga; actualización tras quince minutos", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); app.Microsoft.PhotoAvailable = true;
            var first = await Json(await client.GetAsync("/user/photo")); var photo = first["photo"]!.ToString();
            var page = await Json(await client.GetAsync("/inicio"));
            Check(page["props"]!["photoContext"]!.ToString() == first["contexto"]!.ToString());
            await client.GetAsync("/configuracion"); await client.GetAsync("/inicio");
            Check((await Json(await client.GetAsync("/user/photo")))["photo"]!.ToString() == photo && app.Microsoft.PhotoRequests == 1);
            app.Microsoft.PhotoPixel = Convert.ToBase64String(Convert.FromBase64String(app.Microsoft.PhotoPixel).Concat(new byte[] { 0 }).ToArray());
            app.Clock.Offset = TimeSpan.FromMinutes(16);
            var refreshed = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => client.GetAsync("/user/photo")));
            foreach (var response in refreshed) Check((await Json(response))["photo"]!.ToString() != photo);
            Check(app.Microsoft.PhotoRequests == 2);
        });
        Test("Foto/429, red, timeout y 503 conservan imagen con espera progresiva y límite absoluto", async (app, client) =>
        {
            await Login(app, client); app.Microsoft.PhotoAvailable = true;
            var first = await Json(await client.GetAsync("/user/photo")); var photo = first["photo"]!.ToString();
            app.Clock.Offset = TimeSpan.FromMinutes(16); app.Microsoft.PhotoStatus = HttpStatusCode.TooManyRequests; app.Microsoft.RetrySeconds = 45;
            var limited = await Json(await client.GetAsync("/user/photo"));
            Check(limited["photo"]!.ToString() == photo && limited["estado"]!.ToString() == "conservada");
            Check(limited["vigenteHasta"]!.ToString() == first["vigenteHasta"]!.ToString());
            Check(DateTimeOffset.Parse(limited["renuevaEn"]!.ToString()) - DateTimeOffset.Parse(limited["servidorEn"]!.ToString()) >= TimeSpan.FromSeconds(45));
            await client.GetAsync("/user/photo"); Check(app.Microsoft.PhotoRequests == 2);
            app.Clock.Offset = TimeSpan.FromMinutes(17); app.Microsoft.PhotoStatus = null; app.Microsoft.PhotoError = "network";
            var network = await Json(await client.GetAsync("/user/photo")); Check(network["photo"]!.ToString() == photo);
            Check(DateTimeOffset.Parse(network["renuevaEn"]!.ToString()) - DateTimeOffset.Parse(network["servidorEn"]!.ToString()) >= TimeSpan.FromSeconds(60));
            app.Clock.Offset = TimeSpan.FromMinutes(19); app.Microsoft.PhotoError = "timeout";
            Check((await Json(await client.GetAsync("/user/photo")))["photo"]!.ToString() == photo);
            app.Clock.Offset = TimeSpan.FromMinutes(23); app.Microsoft.PhotoError = null; app.Microsoft.PhotoStatus = HttpStatusCode.ServiceUnavailable;
            Check((await Json(await client.GetAsync("/user/photo")))["photo"]!.ToString() == photo);
            app.Clock.Offset = TimeSpan.FromMinutes(61);
            var expired = await Json(await client.GetAsync("/user/photo")); Check(expired["photo"] is null && expired["estado"]!.ToString() == "temporal");
        });
        Test("Foto/ausencia confirmada retira imagen anterior y no se confunde con indisponibilidad", async (app, client) =>
        {
            await Login(app, client); app.Microsoft.PhotoAvailable = true;
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is not null);
            app.Clock.Offset = TimeSpan.FromMinutes(16); app.Microsoft.PhotoAvailable = false;
            var missing = await Json(await client.GetAsync("/user/photo")); Check(missing["photo"] is null && missing["estado"]!.ToString() == "ausente");
            await client.GetAsync("/user/photo"); Check(app.Microsoft.PhotoRequests == 2);
            app.Clock.Offset = TimeSpan.FromMinutes(19); app.Microsoft.PhotoAvailable = true;
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is not null);
        });
        Test("Foto/renovación de token temporal conserva imagen; cifrado de otro entorno la retira sin alterar tokens", async (app, client) =>
        {
            await Login(app, client); app.Microsoft.PhotoAvailable = true;
            Check((await Json(await client.GetAsync("/user/photo")))["photo"] is not null);
            await database.Sql("UPDATE ms_graph_tokens SET expires='0'");
            app.Clock.Offset = TimeSpan.FromMinutes(16); app.Microsoft.RefreshStatus = HttpStatusCode.ServiceUnavailable;
            Check((await Json(await client.GetAsync("/user/photo")))["estado"]!.ToString() == "conservada");
            var foreign = new ProtectedValues(new EphemeralDataProtectionProvider()).Protect("graph-access", "SYNTHETIC_FOREIGN_TOKEN");
            await database.Sql("UPDATE ms_graph_tokens SET access_token=$1", foreign);
            app.Clock.Offset = TimeSpan.FromMinutes(17); app.Microsoft.RefreshStatus = null;
            var failure = await Json(await client.GetAsync("/user/photo")); Check(failure["photo"] is null && failure["estado"]!.ToString() == "no_disponible");
            Check((await database.Scalar("SELECT access_token FROM ms_graph_tokens"))!.ToString() == foreign && app.Microsoft.PhotoRequests == 1);
            Check(app.Diagnostics.Causes.Contains("token_protection") && app.Diagnostics.Causes.All(c => !c.Contains("SYNTHETIC_")));
        });
        Test("Foto/respuesta lenta revalida revocación local y contexto de representación", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); app.Microsoft.PhotoAvailable = true;
            app.Microsoft.PhotoEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            app.Microsoft.PhotoContinue = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = client.GetAsync("/user/photo"); await app.Microsoft.PhotoEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Check((await StartRepresentation(client)).IsSuccessStatusCode);
            app.Microsoft.PhotoContinue.TrySetResult(); Check((await pending).StatusCode == HttpStatusCode.Conflict);
            await Context(client); Check((await Json(await client.GetAsync("/user/photo")))["photo"] is null);
            // Otra sesión válida obtiene su foto del caché; revocarla no reutiliza permisos anteriores.
            using var second = app.Client(); await Login(app, second);
            Check((await Json(await second.GetAsync("/user/photo")))["photo"] is not null);
            await database.Sql("DELETE FROM tdv2_sessions");
            Check((await second.GetAsync("/user/photo")).StatusCode == HttpStatusCode.Unauthorized);
        });
        Test("Foto/logout durante renovación y cabecera de contexto anterior no entregan imagen", async (app, client) =>
        {
            await Login(app, client); app.Microsoft.PhotoAvailable = true;
            var before = await Json(await client.GetAsync("/user/photo"));
            app.Clock.Offset = TimeSpan.FromMinutes(16);
            app.Microsoft.PhotoEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            app.Microsoft.PhotoContinue = new(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = client.GetAsync("/user/photo"); await app.Microsoft.PhotoEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await database.Sql("DELETE FROM tdv2_sessions"); app.Microsoft.PhotoContinue.TrySetResult();
            Check((await pending).StatusCode == HttpStatusCode.Unauthorized);
            using var next = app.Client(); await Login(app, next);
            next.DefaultRequestHeaders.Add("X-TDV2-Photo-Context", before["contexto"]!.ToString());
            Check((await next.GetAsync("/user/photo")).StatusCode == HttpStatusCode.Conflict);
            next.DefaultRequestHeaders.Remove("X-TDV2-Photo-Context");
            Check((await Json(await next.GetAsync("/user/photo")))["photo"] is not null);
        });
        Test("Foto/volver de representación cambia revisión incluso con la misma cuenta Microsoft", async (app, client) =>
        {
            await SetupAccess(database); await Login(app, client); await Csrf(client); app.Microsoft.PhotoAvailable = true;
            var initial = await Json(await client.GetAsync("/user/photo"));
            Check((await StartRepresentation(client)).IsSuccessStatusCode); await Context(client);
            Check((await client.DeleteAsync("/actuar-como-usuario")).IsSuccessStatusCode); await Context(client);
            client.DefaultRequestHeaders.Add("X-TDV2-Photo-Context", initial["contexto"]!.ToString());
            Check((await client.GetAsync("/user/photo")).StatusCode == HttpStatusCode.Conflict);
            client.DefaultRequestHeaders.Remove("X-TDV2-Photo-Context");
            var current = await Json(await client.GetAsync("/user/photo"));
            Check(current["contexto"]!.ToString() != initial["contexto"]!.ToString() && current["photo"]!.ToString() == initial["photo"]!.ToString());
            Check(app.Microsoft.PhotoRequests == 1);
        });
        Test("Foto/desconectar la petición libera la renovación sin borrar la imagen válida", async (app, client) =>
        {
            await Login(app, client); app.Microsoft.PhotoAvailable = true;
            var initial = await Json(await client.GetAsync("/user/photo")); app.Clock.Offset = TimeSpan.FromMinutes(16);
            app.Microsoft.PhotoEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
            app.Microsoft.PhotoContinue = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = new CancellationTokenSource();
            var pending = client.GetAsync("/user/photo", cancellation.Token);
            await app.Microsoft.PhotoEntered.Task.WaitAsync(TimeSpan.FromSeconds(10)); cancellation.Cancel();
            try { await pending; Check(false, "La solicitud desconectada no debe completarse."); }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            app.Microsoft.PhotoEntered = null; app.Microsoft.PhotoError = "network";
            var current = await Json(await client.GetAsync("/user/photo"));
            Check(current["photo"]!.ToString() == initial["photo"]!.ToString() && current["estado"]!.ToString() == "conservada");
        });
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
