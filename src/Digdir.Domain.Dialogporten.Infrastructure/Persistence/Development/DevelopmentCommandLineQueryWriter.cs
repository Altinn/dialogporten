using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Development;

/// <summary>
/// This interceptor writes executed SQL queries with parameters
/// replaced with their actual values to the console output.
/// Useful for development and debugging purposes.
/// Add to the DbContext only in development environment like so:
/// <code>optionsBuilder.AddInterceptors(new DevelopmentCommandLineQueryWriter());</code>
/// </summary>
internal sealed class DevelopmentCommandLineQueryWriter(Action<string>? log = null) : DbCommandInterceptor
{
    private readonly Action<string> _log = log ?? Console.WriteLine;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        _log($"{ToExecutableSql(command)}{Environment.NewLine}");
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        _log($"{ToExecutableSql(command)}{Environment.NewLine}");
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    private static string ToExecutableSql(DbCommand command)
    {
        var map = command.Parameters
            .Cast<DbParameter>()
            .ToDictionary(
                p => NormalizeName(p.ParameterName),
                p => FormatParameterValue(p.Value),
                StringComparer.OrdinalIgnoreCase);

        // Build a single regex matching all parameter names at once
        // Handles @, :, $, ? prefixes
        var pattern = $@"(?<!\w)(?:@|:|\$|\?)?(?:{string.Join("|", map.Keys.Select(Regex.Escape))})(?!\w)";

        // Single pass replacement
        return Regex.Replace(
            command.CommandText,
            pattern,
            m => map.TryGetValue(NormalizeName(m.Value), out var val)
                ? val
                : m.Value);
    }

    [SuppressMessage("Style", "IDE0055:Fix formatting")]
    private static string NormalizeName(string raw) => raw switch
    {
        ['@', ..] or [':', ..] or ['$', ..] or ['?', ..] => raw[1..],
        _ => raw
    };

    private static string FormatParameterValue(object? value, bool nested = false)
    {
        var q = nested ? '"' : '\'';
        return value == null || value == DBNull.Value
            ? "NULL"
            : value switch
            {
                string s => $"{q}{s.Replace("'", "''")}{q}",
                DateTime dt => $"{q}{dt.ToUniversalTime():O}{q}",
                DateTimeOffset dt => $"{q}{dt.ToUniversalTime():O}{q}",
                bool b => b ? "TRUE" : "FALSE",
                Guid g => $"{q}{g}{q}",
                IEnumerable enumerable =>
                    $"'{{{string.Join(',', enumerable.Cast<object?>().Select(x => FormatParameterValue(x, true)))}}}'",
                _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL"
            };
    }
}
