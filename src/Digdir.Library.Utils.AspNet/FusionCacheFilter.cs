using System.Diagnostics;

namespace Digdir.Library.Utils.AspNet;

public sealed class FusionCacheFilter : OpenTelemetry.BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (activity.Source.Name.Contains("FusionCache", StringComparison.OrdinalIgnoreCase))
        {
            var isError = activity.Tags.Any(t => t is { Key: "otel.status_code", Value: "ERROR" });
            if (!isError)
            {
                activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
                return;
            }
        }

        base.OnEnd(activity);
    }
}
