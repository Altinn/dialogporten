using System.Diagnostics;

namespace Digdir.Library.Utils.AspNet;

public class PostgresExceptionFilter : OpenTelemetry.BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Kind == ActivityKind.Internal &&
            activity.Tags.Any(t => t is { Key: "exception.type", Value: "Npgsql.PostgresException" }))
        {
            activity.IsAllDataRequested = false;
        }
        else
        {
            base.OnEnd(activity);
        }
    }
}
