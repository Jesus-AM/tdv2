using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.UserSecrets;
using Npgsql;
using Tdv2.Security;
using Tdv2.Synchronization;

// Offline configuration check. Never opens a connection or prints configuration values.
try
{
    var workspace = Path.GetFullPath(args[0]);
    var appAssembly = typeof(MicrosoftSettings).Assembly;
    var id = appAssembly.GetCustomAttribute<UserSecretsIdAttribute>()!.UserSecretsId;
    using var launch = JsonDocument.Parse(File.ReadAllText(Path.Combine(workspace, "tdv2/Properties/launchSettings.json")));
    var environment = launch.RootElement.GetProperty("profiles").GetProperty("https")
        .GetProperty("environmentVariables").GetProperty("ASPNETCORE_ENVIRONMENT").GetString();
    Require(environment == "Development");
    var production = args.Length > 1 && args[1] == "--production";
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        ApplicationName = appAssembly.GetName().Name,
        ContentRootPath = Path.Combine(workspace, "tdv2"),
        EnvironmentName = production ? "Production" : environment,
        Args = []
    });
    string[] keys = ["Microsoft:TenantId", "Microsoft:ClientId", "Microsoft:ClientSecret", "Microsoft:PublicOrigin",
        "ConnectionStrings:Tdv2", "ConnectionStrings:Nexo", "ConnectionStrings:Sii", "ConnectionStrings:Ilda", "Synchronization:IldaEnabled"];
    var configuration = (IConfigurationRoot)builder.Configuration;
    if (production)
    {
        Require(!configuration.Providers.OfType<JsonConfigurationProvider>().Any(p => p.Source.Path == "secrets.json"));
        Require(keys.Where(k => k != "Synchronization:IldaEnabled").All(k => string.IsNullOrEmpty(configuration[k])));
        Console.WriteLine("PASS: Production no carga User Secrets; las credenciales deben aportarse externamente.");
        return 0;
    }
    foreach (var key in keys)
    {
        var winner = configuration.Providers.LastOrDefault(p => p.TryGet(key, out _));
        Require(winner is JsonConfigurationProvider { Source.Path: "secrets.json" });
        Require(!string.IsNullOrWhiteSpace(configuration[key]));
    }
    var microsoft = configuration.GetSection("Microsoft").Get<MicrosoftSettings>()!;
    microsoft.Validate();
    Require(microsoft.Callback == "https://localhost:7136/connect");
    Require(!configuration.GetValue<bool>("Synchronization:IldaEnabled"));
    using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(workspace, ".artifacts/local-validation/environment.json")));
    var local = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Tdv2"));
    Require(local.Host == "127.0.0.1" && local.Database == "tdv2_local_validation" && local.Username == "tdv2_local_app"
        && local.Port == manifest.RootElement.GetProperty("port").GetInt32());
    _ = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Nexo"));
    var sources = new SourceConnections(configuration);
    using var sii = sources.Create("sii");
    using var ilda = sources.Create("ilda");
    // A reversible edit is supplied only by the verification script, never by web requests.
    if (args.Length > 1) Require(microsoft.ClientId == args[1]);
    var report = new
    {
        utc = DateTimeOffset.UtcNow, environment, userSecretsId = id,
        effectiveProvider = "secrets.json", keys, localDatabasePreserved = true,
        localCallbackPreserved = true, ildaDisabled = true,
        providerConnectionStringsParsed = true, institutionalConnectionsAttempted = false,
        editedValueObserved = args.Length > 1, visualStudioDebuggerExercised = false
    };
    File.WriteAllText(Path.Combine(workspace, ".artifacts/local-validation/user-secrets-verification.json"),
        JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine("PASS: 9 claves efectivas desde User Secrets; conexiones analizadas sin abrirlas; base local, retorno e ILDA conservados.");
    if (args.Length > 1) Console.WriteLine("PASS: cambio de configuracion observado sin sustitucion por DPAPI.");
    return 0;
}
catch
{
    Console.WriteLine("FAIL: configuracion local no verificada; valores y detalles de excepcion omitidos.");
    return 1;
}
static void Require(bool condition) { if (!condition) throw new InvalidOperationException(); }
