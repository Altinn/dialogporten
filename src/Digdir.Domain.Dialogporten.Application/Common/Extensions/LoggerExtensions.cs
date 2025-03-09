using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions;

/// <summary>
/// Extension methods for ILogger to time operations
/// </summary>
public static class LoggerExtensions
{
    private static readonly LogLevel LogLevel = LogLevel.Debug;

    // Define log message templates as constants to avoid string allocations
    private static readonly Action<ILogger, string, Exception?> OperationStarted =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1337, nameof(TimeOperation)),
            "[TimeOperation] Operation '{OperationName}' started.");

    private static readonly Action<ILogger, string, double, Exception?> OperationCompleted =
        LoggerMessage.Define<string, double>(
            LogLevel.Information,
            new EventId(1338, nameof(TimeOperation)),
            "[TimeOperation] Operation '{OperationName}' completed in {ElapsedMilliseconds}ms.");

    /// <summary>
    /// Creates a disposable timer that logs the execution time of an operation
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="operationName">Name of the operation being timed</param>
    /// <returns>A disposable object that, when disposed, will log the execution time</returns>
    public static IDisposable TimeOperation(
        this ILogger logger,
        string operationName)
    {
        // First check if the log level is enabled to avoid unnecessary overhead
        if (!logger.IsEnabled(LogLevel))
        {
            return new NoOpTimer(); // Return a no-op implementation if logging is not enabled
        }

        return new OperationTimer(logger, operationName);
    }

    /// <summary>
    /// No-op implementation of IDisposable that does nothing, used when logging is disabled
    /// </summary>
    private sealed class NoOpTimer : IDisposable
    {
        public void Dispose() { }
    }

    /// <summary>
    /// A disposable timer for logging operation execution times
    /// </summary>
    private sealed class OperationTimer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public OperationTimer(ILogger logger, string operationName)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();

            OperationStarted(logger, operationName, null);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _stopwatch.Stop();

            OperationCompleted(_logger, _operationName, _stopwatch.Elapsed.TotalMilliseconds, null);
        }
    }
}
