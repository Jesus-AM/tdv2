using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Tdv2.Domain;
using Tdv2.Infrastructure;
using Tdv2.Security;
using Tdv2.Services;
namespace Tdv2.Controllers;

/// <summary>Canal de consulta. Ningún mensaje SignalR concede una reserva ni escribe respuestas.</summary>
public sealed class FormHub(IServiceScopeFactory scopes, FormNotifications notifications, IHttpContextAccessor accessor,
    ILogger<FormHub> logger) : Hub
{
    public async IAsyncEnumerable<object> Watch(string ur, string context, Guid tab,
        [EnumeratorCancellation] CancellationToken cancellation)
    {
        if (string.IsNullOrEmpty(ur) || ur.Length > 32 || tab == Guid.Empty) throw new HubException("Solicitud no válida.");
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation, Context.ConnectionAborted);
        var token = lifetime.Token;
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = token.Register(() => stopped.TrySetResult());
        using var periodic = new PeriodicTimer(TimeSpan.FromSeconds(15));
        Task<bool>? changed = null, tick = null;
        var principal = Context.User!; long? revision = null;
        var subscription = notifications.Subscribe(ur);
        try
        {
            while (!token.IsCancellationRequested)
            {
                object? result = null;
                // El catch no contiene yield return: C# no permite emitir valores desde un try con catch.
                try
                {
                    await using (var scope = scopes.CreateAsyncScope())
                    {
                        var previous = accessor.HttpContext;
                        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider, User = principal, RequestAborted = token };
                        http.Request.Method = "GET"; http.Request.Path = "/formatos/" + ur;
                        accessor.HttpContext = http;
                        try
                        {
                            var state = scope.ServiceProvider.GetRequiredService<AccessState>();
                            var current = await state.Load(token);
                            if (current.Key != context || revision is not null && current.Revision != revision)
                                throw new DomainProblem(409, "El contexto cambió.");
                            revision ??= current.Revision;
                            var db = scope.ServiceProvider.GetRequiredService<Tdv2DbContext>();
                            // El principal de una conexión larga puede sobrevivir al logout: se verifica su ticket en cada entrega.
                            if (state.SessionHash is null || !await db.SessionTickets.AnyAsync(s => s.Id == state.SessionHash && s.ExpiresAt > DateTime.UtcNow, token))
                                throw new DomainProblem(401, "La sesión terminó.");
                            result = await scope.ServiceProvider.GetRequiredService<FormEditingService>().Read(ur, tab);
                        }
                        catch (Exception e) when (e is not OperationCanceledException)
                        {
                            // Una falla real termina el stream; no se confunde con el intervalo ni se silencia.
                            // Registrar sólo el tipo evita filtrar SQL, OAuth o credenciales en el diagnóstico.
                            if (e is not DomainProblem) logger.LogError("Fallo al verificar captura colaborativa. Tipo: {ErrorType}", e.GetType().Name);
                            throw new HubException("No se pudo confirmar el acceso vigente. Actualiza el estado antes de continuar.");
                        }
                        finally { accessor.HttpContext = previous; }
                    }
                }
                catch (OperationCanceledException error) when (
                    token.IsCancellationRequested && error.CancellationToken == token
                    || cancellation.IsCancellationRequested && error.CancellationToken == cancellation
                    || Context.ConnectionAborted.IsCancellationRequested && error.CancellationToken == Context.ConnectionAborted)
                { break; }
                if (token.IsCancellationRequested) break;
                yield return result;
                // La comprobación periódica detecta revocaciones y cambios de otra instancia.
                // Consultar NO renueva reservas; únicamente una actividad explícita del editor lo hace.
                // El intervalo no cancela una lectura: no genera excepciones periódicas en el depurador.
                // Hay sólo una espera pendiente por fuente; terminar completa el canal y el temporizador.
                changed ??= subscription.Reader.WaitToReadAsync().AsTask();
                tick ??= periodic.WaitForNextTickAsync().AsTask();
                await Task.WhenAny(changed, tick, stopped.Task);
                if (token.IsCancellationRequested) break;
                if (changed.IsCompleted)
                {
                    if (!await changed) break;
                    while (subscription.Reader.TryRead(out _)) { }
                    changed = null;
                }
                if (tick.IsCompleted) { if (!await tick) break; tick = null; }
            }
        }
        finally { notifications.Remove(subscription.Id); }
    }
}
