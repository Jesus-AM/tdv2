using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Tdv2.Infrastructure;

public static class Tdv2DatabaseOptions
{
    public static void Configure(DbContextOptionsBuilder options, IConfiguration configuration)
    {
        var value = configuration.GetConnectionString("Tdv2");
        // Permite generar migraciones sin credenciales; sólo abrir la conexión exige configurarla.
        var connection = string.IsNullOrWhiteSpace(value) ? null : new NpgsqlConnectionStringBuilder(value)
        {
            IncludeErrorDetail = false,
            LogParameters = false
        }.ConnectionString;
        options.UseNpgsql(connection, postgres => postgres.MigrationsHistoryTable("__EFMigrationsHistory", "public"));
        // No habilitar reintentos automáticos: algunos casos de uso canjean tokens o llaman Nexo.
        options.EnableSensitiveDataLogging(false).EnableDetailedErrors(false);
    }
}
