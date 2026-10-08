using Microsoft.Extensions.Caching.Memory;
using Tdv2.Integrations.Microsoft;

namespace Tdv2.Services;

public sealed record ParticipantPhoto(string? Photo, DateTimeOffset RenuevaEn, DateTimeOffset? VigenteHasta = null,
    string Estado = "ausente", DateTimeOffset? ServidorEn = null, string? Contexto = null);

/// <summary>Miniaturas privadas, sin permisos cacheados; vigencia máxima desde la última comprobación correcta.</summary>
public sealed class MicrosoftPhotoCache(TimeProvider clock) : IDisposable
{
    public static readonly TimeSpan FreshFor = TimeSpan.FromMinutes(15), MaximumAge = TimeSpan.FromHours(1);
    private sealed record Entry(string? Photo, DateTimeOffset? ValidUntil, DateTimeOffset RetryAt, int Failures, string Status);
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 32 * 1024 * 1024 });
    private readonly SemaphoreSlim[] gates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public async Task<ParticipantPhoto> Get(string identity, Func<CancellationToken, Task<MicrosoftPhotoResult>> load, CancellationToken ct)
    {
        var gate = gates[Math.Abs(identity.GetHashCode() % gates.Length)];
        await gate.WaitAsync(ct);
        try
        {
            var now = clock.GetUtcNow();
            cache.TryGetValue<Entry>(identity, out var saved);
            if (saved is not null && saved.RetryAt > now) return Reply(saved, now);
            var result = await load(ct); now = clock.GetUtcNow();
            Entry entry;
            if (result.Status == MicrosoftPhotoStatus.Found)
                entry = new(result.Photo, now + MaximumAge, now + FreshFor, 0, "actual");
            else if (result.Status == MicrosoftPhotoStatus.Temporary)
            {
                var failures = Math.Min(10, (saved?.Failures ?? 0) + 1);
                var seconds = Math.Clamp(Math.Max(30 * Math.Pow(2, failures - 1), result.RetryAfter?.TotalSeconds ?? 0), 30, 900);
                // Un reintento fallido NO mueve el límite absoluto de la imagen anterior.
                var retained = saved?.ValidUntil > now ? saved.Photo : null;
                entry = new(retained, retained is null ? null : saved!.ValidUntil, now.AddSeconds(seconds), failures,
                    retained is null ? "temporal" : "conservada");
            }
            else
                entry = new(null, null, now.AddMinutes(2), 0, result.Status == MicrosoftPhotoStatus.Missing ? "ausente" : "no_disponible");
            var lifetime = entry.ValidUntil is { } until && until > entry.RetryAt ? until - now : entry.RetryAt - now;
            cache.Set(identity, entry, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime,
                Size = (entry.Photo?.Length ?? 0) * 2L + 512 });
            return Reply(entry, now);
        }
        finally { gate.Release(); }
    }
    private static ParticipantPhoto Reply(Entry entry, DateTimeOffset now) => new(
        entry.ValidUntil > now ? entry.Photo : null, entry.RetryAt, entry.ValidUntil,
        entry.Photo is not null && entry.ValidUntil <= now ? "temporal" : entry.Status, now);
    public void Dispose() { cache.Dispose(); foreach (var gate in gates) gate.Dispose(); }
}
