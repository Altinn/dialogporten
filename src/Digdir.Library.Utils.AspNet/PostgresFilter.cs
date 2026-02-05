using System.Diagnostics;
using Digdir.Domain.Dialogporten.Infrastructure;
using Microsoft.Extensions.Options;

namespace Digdir.Library.Utils.AspNet;

public class PostgresFilter : OpenTelemetry.BaseProcessor<Activity>
{
    private readonly IOptionsMonitor<InfrastructureSettings> _optionsMonitor;

    public PostgresFilter(IOptionsMonitor<InfrastructureSettings> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
        ArgumentNullException.ThrowIfNull(optionsMonitor);
    }

    public override void OnEnd(Activity activity)
    {
        if (!activity.Source.Name.StartsWith(Constants.Npgsql, StringComparison.OrdinalIgnoreCase))
        {
            base.OnEnd(activity);
            return;
        }

        var currentOptions = _optionsMonitor.CurrentValue;

        // Add parameter information to the activity if enabled
        if (currentOptions.EnableSqlParametersLogging)
        {
            AddParameterInformation(activity);
        }

        if (!currentOptions.EnableSqlStatementLogging && activity.Tags.IsSuccessfulSqlStatementActivity())
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }

        if (activity.Tags.ContainsUniqueConstraintError())
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }

        base.OnEnd(activity);
    }

    private static void AddParameterInformation(Activity activity)
    {
        // Npgsql's OpenTelemetry instrumentation adds parameter information to activity tags
        // when EnableParameterLogging() is called on the data source.
        // Parameters are added with keys like "db.query.parameter.<name>" following OpenTelemetry semantic conventions
        var parameters = activity.Tags
            .Where(t => t.Key.StartsWith(Constants.DbQueryParameterPrefix, StringComparison.Ordinal) ||
                       t.Key.StartsWith(Constants.DbStatementParameterPrefix, StringComparison.Ordinal))
            .OrderBy(t => t.Key)
            .Select(t =>
            {
                var paramName = t.Key.Replace(Constants.DbQueryParameterPrefix, "")
                                    .Replace(Constants.DbStatementParameterPrefix, "@p");
                return $"{paramName}={t.Value}";
            })
            .ToList();

        if (parameters.Count > 0)
        {
            // Add a consolidated parameter tag for easier viewing in logs
            activity.SetTag(Constants.DbQueryParameters, string.Join("; ", parameters));
        }
    }
}

internal static class ActivityTagsExtensions
{
    // "23505" is the PostgreSQL error code for unique constraint violation
    private const string UniqueViolationErrorCode = "23505";
    // "23503" is the PostgreSQL error code for foreign key violation
    private const string ForeignKeyViolationErrorCode = "23503";

    // This mutes Slack notifications and error logging in AppInsights
    public static bool ContainsUniqueConstraintError(this IEnumerable<KeyValuePair<string, string?>> tags) =>
        tags.Any(t => t is { Key: Constants.OtelStatusCode, Value: Constants.Error }) &&
        tags.Any(t => t is
        {
            Key: Constants.OtelStatusDescription,
            Value: UniqueViolationErrorCode or ForeignKeyViolationErrorCode
        });

    public static bool IsSuccessfulSqlStatementActivity(this IEnumerable<KeyValuePair<string, string?>> tags) =>
        tags.Any(t => t is { Key: Constants.DbStatement }) &&
        tags.Any(t => t is { Key: Constants.OtelStatusCode, Value: Constants.Ok });
}
