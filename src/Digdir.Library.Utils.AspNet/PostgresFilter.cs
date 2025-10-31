using System.Diagnostics;

namespace Digdir.Library.Utils.AspNet;

public class PostgresFilter : OpenTelemetry.BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (!activity.Source.Name.StartsWith(Constants.Npgsql, StringComparison.OrdinalIgnoreCase))
        {
            base.OnEnd(activity);
            return;
        }

        if (activity.Tags.IsSuccessfulSqlStatementActivity() ||
                activity.Tags.ContainsUniqueConstraintError())
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }

        base.OnEnd(activity);
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
