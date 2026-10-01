using Tdv2.Domain;

namespace Tdv2.Services;

/// <summary>Datos de una pantalla; el transporte añade sesión y CSRF sin serializar secretos.</summary>
public sealed record PageData(string Component, Dictionary<string, object?> Props, Profile? Profile = null);
