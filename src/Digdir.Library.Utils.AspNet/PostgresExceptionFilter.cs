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
    public static bool ContainsUniqueConstraintError(this IEnumerable<KeyValuePair<string, string?>> tags) =>
        tags.Any(t => t is { Key: "otel.status_code", Value: "ERROR" }) &&
        tags.Any(t => t.Key == "otel.status_description" && t.Value?.Contains("23505") == true);
}
