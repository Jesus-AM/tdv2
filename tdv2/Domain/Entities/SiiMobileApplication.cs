namespace Tdv2.Domain.Entities;

/// <summary>Réplica de las columnas autorizadas; Id conserva el identity del origen.</summary>
public sealed class SiiMobileApplication
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Definition { get; set; }
    public int? EmployeeResponsible { get; set; }
    public int? ProgrammerResponsible { get; set; }
    public int UnitId { get; set; }
    public bool Active { get; set; }
    public int? GeneralDirectorEmployee { get; set; }
    // Disponibilidad de la réplica, distinta de ACTIVA, que sólo se lee en la fuente.
    public bool Present { get; set; }
    public DateTime SynchronizedAt { get; set; }
}
