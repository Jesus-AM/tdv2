namespace Tdv2.Domain.Entities;

public sealed class ApplicationCatalogEntry
{
    public string Category { get; set; } = null!;
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    // Identificador estable del archivo versionado. Los históricos sin correspondencia no lo tienen.
    public string? ListKey { get; set; }
    public bool Available { get; set; }
}

public sealed class ApplicationCatalogImport
{
    public string Category { get; set; } = null!;
    public string Edition { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime ImportedAt { get; set; }
    public string Result { get; set; } = "{}";
}
