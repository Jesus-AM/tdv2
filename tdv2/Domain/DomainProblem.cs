namespace Tdv2.Domain;

// Sólo mensajes públicos controlados; nunca envolver errores SQL o de proveedores.
public sealed class DomainProblem(int status, string message, object? details = null) : Exception(message)
{
    public int Status { get; } = status;
    public object? Details { get; } = details;
}
