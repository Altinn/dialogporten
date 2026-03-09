using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using Npgsql;

namespace Digdir.Library.Utils.AspNet;

internal sealed class PostgresSerilogFilter : ILogEventFilter
{
    public bool IsEnabled(LogEvent logEvent) =>
        !HasHandledPostgresException(logEvent.Exception);

    private static bool HasHandledPostgresException(Exception? exception)
    {
        for (var currentEx = exception; currentEx is not null; currentEx = currentEx.InnerException)
        {
            if (currentEx is PostgresException postgresException &&
                HandledPostgresErrorCodes.All.Contains(postgresException.SqlState))
            {
                return true;
            }

            if (currentEx is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.InnerExceptions)
                {
                    if (HasHandledPostgresException(innerException))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}

public static class PostgresSerilogFilterExtensions
{
    public static LoggerFilterConfiguration WithHandledPostgresExceptionFilter(this LoggerFilterConfiguration filterConfiguration) =>
        filterConfiguration.With(new PostgresSerilogFilter());
}
