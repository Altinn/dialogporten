using System.Diagnostics;

namespace Digdir.Library.Utils.AspNet;

public class PostgresExceptionFilter : OpenTelemetry.BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Tags.ContainsUniqueConstraintError())
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
        }
        else
        {
            base.OnEnd(activity);
        }
    }
}

internal static class ActivityExtensions
{
    // "23505" is the PostgreSQL error code for unique constraint violation
    // "23503" is the PostgreSQL error code for foreign key violation
    // This mutes Slack notifications and error logging in AppInsights
    public static bool ContainsUniqueConstraintError(this IEnumerable<KeyValuePair<string, string?>> tags) =>
        tags.Any(t => t is { Key: "otel.status_code", Value: "ERROR" }) &&
        tags.Any(t => t is { Key: "otel.status_description", Value: "23505" or "23503" });
}
