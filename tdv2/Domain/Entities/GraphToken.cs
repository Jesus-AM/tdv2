namespace Tdv2.Domain.Entities;

/// <summary>Entidad persistida en ms_graph_tokens; no se envía directamente a React.</summary>
public sealed class GraphToken
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? Email { get; set; }
    public string AccessToken { get; set; } = "";
    public string? RefreshToken { get; set; }
    public string Expires { get; set; } = "";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
