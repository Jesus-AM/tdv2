namespace Tdv2.Domain;

// Only fixed, public messages belong here. Never wrap provider/SQL exception messages.
public sealed class DomainProblem(int status, string message, object? details = null) : Exception(message)
{
    public int Status { get; } = status;
    public object? Details { get; } = details;
}
