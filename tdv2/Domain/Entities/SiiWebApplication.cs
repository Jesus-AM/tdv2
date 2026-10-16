namespace Tdv2.Domain.Entities;

public sealed class SiiWebApplication
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Definition { get; set; }
    public int? EmployeeResponsible { get; set; }
    public int? ProgrammerResponsible { get; set; }
    public string? Url { get; set; }
    public bool Active { get; set; }
    public int? GeneralDirectorEmployee { get; set; }
    // Dato de origen, no FK: el padre puede no estar entre las aplicaciones activas.
    public int? ParentApplicationId { get; set; }
    public bool Present { get; set; }
    public DateTime SynchronizedAt { get; set; }
}
