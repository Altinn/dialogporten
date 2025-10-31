using System.Diagnostics;

namespace Digdir.Library.Utils.AspNet;

public sealed class FusionCacheFilter : OpenTelemetry.BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (!activity.Source.Name.Contains(Constants.FusionCache, StringComparison.OrdinalIgnoreCase))
        {
            base.OnEnd(activity);
            return;
        }

        var isError = activity.Tags.Any(t => t is { Key: Constants.OtelStatusCode, Value: Constants.Error });
        if (!isError)
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }

        base.OnEnd(activity);
    }
}
