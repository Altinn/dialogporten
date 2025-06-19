using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions;

/// <summary>
/// Extension methods for ILogger to time operations
/// </summary>
public static partial class LoggerExtensions
{
    private const LogLevel Level = LogLevel.Debug;

    /// <summary>
    /// Creates a disposable timer that logs the execution time of an operation
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="operationName">Name of the operation being timed</param>
    /// <returns>A disposable object that, when disposed, will log the execution time</returns>
    public static IDisposable TimeOperation(
        this ILogger logger,
        string operationName) =>
        logger.IsEnabled(Level)
            ? new OperationTimer(logger, operationName)
            : new NoOpTimer();

    /// <summary>
    /// No-op implementation of IDisposable that does nothing, used when logging is disabled
    /// </summary>
    private struct NoOpTimer : IDisposable
    {
        public readonly void Dispose() { }
    }

    /// <summary>
    /// A disposable timer for logging operation execution times
    /// </summary>
    private sealed class OperationTimer : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly long _startTimestamp;
        private bool _disposed;

        public OperationTimer(ILogger logger, string operationName)
        {
            _logger = logger;
            _operationName = operationName;
            _startTimestamp = Stopwatch.GetTimestamp();
            OperationStarted(logger, _operationName);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            OperationCompleted(_logger, _operationName, Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds);
        }
    }

    [LoggerMessage(
        Level = Level,
        EventId = 1337,
        EventName = nameof(TimeOperation) + "Started",
        Message = "[TimeOperation] Operation '{OperationName}' started.")]
    private static partial void OperationStarted(ILogger logger, string operationName);

    [LoggerMessage(
        Level = Level,
        EventId = 1338,
        EventName = nameof(TimeOperation) + "Completed",
        Message = "[TimeOperation] Operation '{OperationName}' completed in {ElapsedMilliseconds}ms.")]
    private static partial void OperationCompleted(ILogger logger, string operationName, double elapsedMilliseconds);
}
