using Serilog.Core;
using Serilog.Events;
using Serilog.Configuration;
using Npgsql;
using Serilog;

namespace Digdir.Library.Utils.AspNet;

internal sealed class PostgresSerilogFilter : ILogEventFilter
{
    public bool IsEnabled(LogEvent logEvent) =>
        !HasHandledPostgresException(logEvent.Exception);

    private static bool HasHandledPostgresException(Exception? exception)
    {
        for (var currentEx = exception; currentEx is not null; currentEx = currentEx.InnerException)
        {
            switch (currentEx)
            {
                case PostgresException postgresException when
                    HandledPostgresErrorCodes.All.Contains(postgresException.SqlState):
                    return true;
                case AggregateException aggregateException:
                    {
                        if (aggregateException.InnerExceptions.Any(HasHandledPostgresException))
                        {
                            return true;
                        }

                        break;
                    }

                default:
                    break;
            }
        }

        return false;
    }
}

public static class PostgresSerilogFilterExtensions
{
    public static LoggerConfiguration WithHandledPostgresExceptionFilter(this LoggerFilterConfiguration filterConfiguration) =>
        filterConfiguration.With(new PostgresSerilogFilter());
}
