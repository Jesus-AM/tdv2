using System.Globalization;
using System.Text.Json.Nodes;
using NodaTime;
namespace Tdv2.Synchronization;

public sealed class SyncOptions
{
    public bool IldaEnabled { get; set; }
    public bool SiiLatestExercise { get; set; } = true;
    public int MaxRows { get; set; } = 100000;
    public long IldaMaxBytes { get; set; } = 134217728;
    public int CommandTimeoutSeconds { get; set; } = 120;
    public void Validate()
    {
        if (MaxRows is < 1 or > 100000 || IldaMaxBytes is < 1 or > 134217728 || CommandTimeoutSeconds is < 1 or > 1200)
            throw new SyncProblem("Los límites de sincronización del servidor no son válidos.");
    }
}
public sealed class SyncProblem(string message) : Exception(message);
public sealed class SyncLeaseLost : Exception;
public interface ICatalogSource
{
    Task<IReadOnlyList<JsonObject>> Read(string source, CancellationToken ct);
}
public sealed record SyncSchedule(int Version, bool Active, int Minutes, string Time, string Zone, bool IncludeIlda)
{
    public static readonly int[] Intervals = [15, 30, 60, 180, 360, 1440];
    public static DateTimeZone ZoneInfo(string zone)
    {
        // Ship the same IANA rules on Windows and Linux, including Ciudad_Juarez.
        return zone.Length <= 64 ? DateTimeZoneProviders.Tzdb.GetZoneOrNull(zone) ?? throw new SyncProblem("Selecciona una zona horaria IANA válida.")
            : throw new SyncProblem("Selecciona una zona horaria IANA válida.");
    }
    public DateTimeOffset Next(DateTimeOffset after)
    {
        if (!Intervals.Contains(Minutes) || !TimeOnly.TryParseExact(Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            throw new SyncProblem("Revisa el intervalo y el horario de sincronización.");
        var zone = ZoneInfo(Zone);
        if (Minutes != 1440) return after.AddMinutes(Minutes);
        var local = Instant.FromDateTimeOffset(after).InZone(zone).Date;
        DateTimeOffset At(LocalDate date) => zone.AtLeniently(date.At(new LocalTime(time.Hour,time.Minute))).ToDateTimeOffset().ToUniversalTime();
        var candidate = At(local);
        return candidate > after ? candidate : At(local.PlusDays(1));
    }
}
