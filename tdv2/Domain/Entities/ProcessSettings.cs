namespace Tdv2.Domain.Entities;

public sealed class ProcessSettings
{
    public int Id { get; set; } = 1;
    public int Version { get; set; } = 1;
    public int[] Levels { get; set; } = [2, 3];
    public string[] ExcludedTypes { get; set; } = ["N"];
    public DateTimeOffset? UpdatedAt { get; set; }
}
