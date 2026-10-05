using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
using Tdv2.Services;
namespace Tdv2.Controllers;

/// <summary>Canal de consulta. Ningún mensaje SignalR concede una reserva ni escribe respuestas.</summary>
public sealed class FormHub(IServiceScopeFactory scopes, FormNotifications notifications, IHttpContextAccessor accessor) : Hub
{
    public async IAsyncEnumerable<object> Watch(string ur, string context, Guid tab,
        [EnumeratorCancellation] CancellationToken cancellation)
    {
        if (string.IsNullOrEmpty(ur) || ur.Length > 32 || tab == Guid.Empty) throw new HubException("Solicitud no válida.");
        var principal = Context.User!; long? revision = null;
        var subscription = notifications.Subscribe(ur);
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                object result;
                await using (var scope = scopes.CreateAsyncScope())
                {
                    var previous = accessor.HttpContext;
                    var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider, User = principal, RequestAborted = cancellation };
                    http.Request.Method = "GET"; http.Request.Path = "/formatos/" + ur;
                    accessor.HttpContext = http;
                    try
                    {
                        var state = scope.ServiceProvider.GetRequiredService<AccessState>();
                        var current = await state.Load(cancellation);
                        if (current.Key != context || revision is not null && current.Revision != revision)
                            throw new DomainProblem(409, "El contexto cambió.");
                        revision ??= current.Revision;
                        var db = scope.ServiceProvider.GetRequiredService<Tdv2DbContext>();
                        // El principal de una conexión larga puede sobrevivir al logout: se verifica su ticket en cada entrega.
                        if (state.SessionHash is null || !await db.SessionTickets.AnyAsync(s => s.Id == state.SessionHash && s.ExpiresAt > DateTime.UtcNow, cancellation))
                            throw new DomainProblem(401, "La sesión terminó.");
                        result = await scope.ServiceProvider.GetRequiredService<FormEditingService>().Read(ur, tab);
                    }
                    catch (Exception e) when (e is not OperationCanceledException)
                    { throw new HubException("No se pudo confirmar el acceso vigente. Actualiza el estado antes de continuar."); }
                    finally { accessor.HttpContext = previous; }
                }
                yield return result;
                // La comprobación periódica detecta revocaciones y cambios de otra instancia.
                // Consultar NO renueva reservas; únicamente una actividad explícita del editor lo hace.
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                try { await subscription.Reader.ReadAsync(timeout.Token); }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested) { }
                while (subscription.Reader.TryRead(out _)) { }
            }
        }
        finally { notifications.Remove(subscription.Id); }
    }
}
