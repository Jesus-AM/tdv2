using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging.Abstractions;

namespace Tdv2.Infrastructure;

/// <summary>Soporta dotnet ef sin construir el sitio, iniciar procesadores ni abrir conexiones.</summary>
public sealed class Tdv2DbContextFactory : IDesignTimeDbContextFactory<Tdv2DbContext>
{
    public Tdv2DbContext CreateDbContext(string[] args)
    {
        var directory = Directory.GetCurrentDirectory();
        var root = File.Exists(Path.Combine(directory, "tdv2.csproj")) ? directory : Path.Combine(directory, "tdv2");
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = root,
            ApplicationName = typeof(Tdv2DbContext).Assembly.GetName().Name
        });
        var options = new DbContextOptionsBuilder<Tdv2DbContext>();
        Tdv2DatabaseOptions.Configure(options, builder.Configuration);
        options.UseLoggerFactory(NullLoggerFactory.Instance);
        return new Tdv2DbContext(options.Options);
    }
}
