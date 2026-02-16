using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common;

internal sealed class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];
    private static readonly IDisposable NoOpScope = new NoopDisposable();

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NoOpScope;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    internal sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
