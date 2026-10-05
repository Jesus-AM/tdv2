using Microsoft.Extensions.Logging;
namespace Tdv2.NativeVerification;

// Sólo se registra en el host sintético. No muestra argumentos HTTP, tokens ni excepciones de proveedor.
internal sealed class SyntheticDiagnostics : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new SafeLogger(categoryName);
    public void Dispose() { }
    private sealed class SafeLogger(string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (level < LogLevel.Warning || state is not IEnumerable<KeyValuePair<string, object?>> values) return;
            foreach (var item in values.Where(p => p.Key is "Type" or "Tipo"))
                Console.WriteLine("SYNTHETIC " + category + " type=" + item.Value);
        }
    }
}
