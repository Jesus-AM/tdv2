using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Tdv2.NativeVerification;

// Sólo en el ejecutable sintético: reproduce una envoltura EF transitoria.
// No presupone la causa de la excepción institucional aún no diagnosticada.
internal sealed class DigitalControlFailure : DbCommandInterceptor
{
    internal bool Enabled;
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    {
        if (Enabled && command.CommandText.Contains("cd_control", StringComparison.Ordinal))
            throw new InvalidOperationException("SYNTHETIC_TRANSIENT_WRAPPER", new NpgsqlException("SYNTHETIC_TRANSPORT", new TimeoutException("SYNTHETIC_PRIVATE_DETAIL")));
        return ValueTask.FromResult(result);
    }
}
