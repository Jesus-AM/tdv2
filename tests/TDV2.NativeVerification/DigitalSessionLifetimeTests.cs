using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Tdv2.Security;

namespace Tdv2.NativeVerification;

internal sealed partial class DigitalTests
{
    private void AddSessionLifetimeTests(List<(string Name,Func<NativeApplication,HttpClient,Task> Work)> cases)
    {
        void Test(string name,Func<NativeApplication,HttpClient,Task> work)=>cases.Add(("Sesión dos horas: "+name,work));
        Test("límite exacto compartido, polling no renueva, conserva respuestas y vuelve al portal",async(app,client)=>{
            var start=DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()).AddMilliseconds(137); app.Clock.FixedUtc=start;
            await Role("administrador"); await Login(app,client); var survey=await CompleteAndSave(client,0);
            var frozen=await Data("SELECT \"AnswersJson\"::text FROM cd_surveys WHERE \"Employee\"='0001'");
            var initial=await Json(await client.GetAsync("/session/status")); var id=initial["id"]!.ToString();
            Check(initial["expires_at"]!.GetValue<long>()-initial["started_at"]!.GetValue<long>()==7200000);
            var cookie=app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get("Cookies");
            Check(!cookie.SlidingExpiration && cookie.ExpireTimeSpan==TimeSpan.FromHours(2));
            foreach(var minute in new[]{30,65,119}) {
                app.Clock.FixedUtc=start.AddMinutes(minute);
                var status=await Json(await client.GetAsync("/session/status")); Check(status["id"]!.ToString()==id && status["expires_at"]!.ToString()==initial["expires_at"]!.ToString());
                await Status(client.GetAsync(Base+"/api/generacion"),200);
            }
            app.Clock.FixedUtc=start.AddHours(2).AddMilliseconds(-1); await Status(client.GetAsync("/session/status"),200);
            app.Clock.FixedUtc=start.AddHours(2);
            var expired=await Json(await client.PutAsJsonAsync(Base+"/api/encuesta",new Tdv2.Competencias.DigitalSave(survey.Version,survey.Profile,survey.Answers,survey.Generation)),401);
            Check(expired["sessionExpired"]!.GetValue<bool>() && expired["redirect"]!.ToString()=="/");
            var apiHtml=new HttpRequestMessage(HttpMethod.Get,Base+"/api/encuesta"); apiHtml.Headers.Accept.ParseAdd("text/html");
            var deniedApi=await client.SendAsync(apiHtml);Check(deniedApi.StatusCode==HttpStatusCode.Unauthorized && deniedApi.Content.Headers.ContentType!.MediaType=="application/json");
            Check(Equals(frozen,await Data("SELECT \"AnswersJson\"::text FROM cd_surveys WHERE \"Employee\"='0001'")));
            Check(Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM tdv2_sessions"))==0);
            var request=new HttpRequestMessage(HttpMethod.Get,"/inicio"); request.Headers.Accept.ParseAdd("text/html");
            var back=await client.SendAsync(request); Check(back.StatusCode==HttpStatusCode.Redirect && back.Headers.Location!.OriginalString=="/");
            await Status(client.GetAsync("/"),200);
        });
        Test("dos sitios y representación no reinician inicio; vencimiento termina sólo su contexto",async(app,client)=>{
            var start=DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());app.Clock.FixedUtc=start;
            await Role("administrador"); await Login(app,client);
            var initial=await Json(await client.GetAsync("/session/status"));
            app.Clock.FixedUtc=start.AddMinutes(70); await Status(client.GetAsync("/inicio"),200);
            await Status(client.PostAsJsonAsync(Base+"/api/representacion",new{email="otra@uacj.mx",motivo="Consulta sintética de permisos"}),200);
            var status=await Json(await client.GetAsync("/session/status"));Check(status["id"]!.ToString()==initial["id"]!.ToString() && status["expires_at"]!.ToString()==initial["expires_at"]!.ToString());
            app.Clock.FixedUtc=start.AddHours(2); await Status(client.GetAsync(Base+"/api/encuesta"),401);
            Check(Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM tdv2_access_contexts"))==0);
            Check(Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM activity_logs WHERE action='logout' AND meta->>'resultado'='confirmado'"))==1);
        });
        Test("403 y 503 no cierran sesión; nueva entrada explícita y petición vieja no cierra sesión nueva",async(app,client)=>{
            var start=DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());app.Clock.FixedUtc=start;
            await Login(app,client);var initial=await Json(await client.GetAsync("/session/status"));
            await Status(client.GetAsync(Base+"/api/gestion-encuestas/personas?q=prueba"),403);await Status(client.GetAsync("/session/status"),200);
            await Nexo("REVOKE SELECT ON nexo_usuarios FROM cd_native_nexo");
            try { await Status(client.GetAsync(Base+"/api/encuesta"),503);await Status(client.GetAsync("/session/status"),200); }
            finally { await Nexo("GRANT SELECT ON nexo_usuarios TO cd_native_nexo"); }
            app.Clock.FixedUtc=start.AddHours(2);await Status(client.GetAsync("/session/status"),401);
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
            await Login(app,client); var next=await Json(await client.GetAsync("/session/status"));Check(next["id"]!.ToString()!=initial["id"]!.ToString());
            await Status(client.PostAsJsonAsync("/session/expire",new{id=initial["id"]!.ToString()}),409);
            client.DefaultRequestHeaders.Add("X-TDV2-Session",initial["id"]!.ToString());
            var stale=await Json(await client.GetAsync(Base+"/api/encuesta"),409);Check(stale["sessionChanged"]!.GetValue<bool>());
            client.DefaultRequestHeaders.Remove("X-TDV2-Session");
            await Status(client.GetAsync("/session/status"),200);
            Check(next["expires_at"]!.GetValue<long>()==start.AddHours(4).ToUnixTimeMilliseconds());
        });
        Test("ticket anterior sin inicio fiable se invalida; no cierra otra sesión más reciente",async(app,client)=>{
            var start=DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()); app.Clock.FixedUtc=start;
            await Login(app,client);
            var crypto=app.Services.GetRequiredService<ProtectedValues>();
            var stored=(string)(await shared.Scalar("SELECT ticket FROM tdv2_sessions"))!;
            var old=Microsoft.AspNetCore.Authentication.TicketSerializer.Default.Deserialize(Convert.FromBase64String(crypto.Unprotect("session",stored)))!;
            old.Properties.Items.Remove(SessionLifetime.StartKey);
            await shared.Sql("UPDATE tdv2_sessions SET ticket=$1",crypto.Protect("session",Convert.ToBase64String(Microsoft.AspNetCore.Authentication.TicketSerializer.Default.Serialize(old))));
            await Status(client.GetAsync("/session/status"),401);
            client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");await Login(app,client);
            app.Clock.FixedUtc=start.AddMinutes(60);using var other=app.Client();await Login(app,other);
            app.Clock.FixedUtc=start.AddHours(2);await Status(client.GetAsync("/session/status"),401);await Status(other.GetAsync("/session/status"),200);
            Check(Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM tdv2_sessions"))==1);
        });
        Test("renovar no amplía inicio/SQL y nunca recrea una sesión revocada",async(app,client)=>{
            var start=DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());app.Clock.FixedUtc=start;
            var principal=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([new(System.Security.Claims.ClaimTypes.Email,"persona@uacj.mx")],"Cookies"));
            var ticket=new Microsoft.AspNetCore.Authentication.AuthenticationTicket(principal,SessionLifetime.Start(principal,app.Clock),"Cookies");
            var store=app.Services.GetRequiredService<PostgresTicketStore>(); var key=await store.StoreAsync(ticket);
            var expiry=await shared.Scalar("SELECT expires_at FROM tdv2_sessions");
            app.Clock.FixedUtc=start.AddHours(1);ticket.Properties.ExpiresUtc=app.Clock.GetUtcNow().AddHours(2);ticket.Properties.IssuedUtc=app.Clock.GetUtcNow();
            await store.RenewAsync(key,ticket);Check(Equals(expiry,await shared.Scalar("SELECT expires_at FROM tdv2_sessions")));
            await store.RemoveAsync(key);await store.RenewAsync(key,ticket);Check(Convert.ToInt64(await shared.Scalar("SELECT count(*) FROM tdv2_sessions"))==0);
        });
        Test("error de generación es 503, conserva sesión y no impide Procedimientos; diagnóstico sin secretos",async(app,client)=>{
            await Login(app,client);var initial=await Json(await client.GetAsync("/session/status"));
            app.DigitalFailure.Enabled=true;
            var failed=await Json(await client.GetAsync(Base+"/api/generacion"),503);
            Check(!failed.ToJsonString().Contains("SYNTHETIC") && failed["sessionExpired"] is null);
            var status=await Json(await client.GetAsync("/session/status"));Check(status["id"]!.ToString()==initial["id"]!.ToString());
            await Status(client.GetAsync("/inicio"),200);app.DigitalFailure.Enabled=false;
            await Status(client.GetAsync(Base+"/api/generacion"),200);
            var writer=new StringWriter();var config=new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(Configuration).Build();
            Check(await DigitalPopulationReadOnly.Generation(config,writer)==0 && writer.ToString().Contains("\"readOnly\":true"));
            var error=DigitalPopulationReadOnly.SafeError(new InvalidOperationException("SYNTHETIC_PRIVATE",new Npgsql.NpgsqlException("SYNTHETIC_CONNECTION",new TimeoutException("SYNTHETIC_SECRET"))));
            Check(error.Contains("wrapper=InvalidOperationException") && error.Contains("CONNECTION_FAILURE") && error.Contains("TIMEOUT") && !error.Contains("SYNTHETIC"));
            var transport=DigitalPopulationReadOnly.SafeError(new Npgsql.NpgsqlException("SYNTHETIC_SECRET",new IOException("SYNTHETIC_PRIVATE")));
            Check(transport.Contains("CONNECTION_FAILURE") && transport.Contains("IOException") && !transport.Contains("CONFIG_IO") && !transport.Contains("SYNTHETIC"));
        });
    }
}
