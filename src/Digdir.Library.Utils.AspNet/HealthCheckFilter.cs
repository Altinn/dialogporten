using System.Diagnostics;

namespace Digdir.Library.Utils.AspNet;

public class HealthCheckFilter : OpenTelemetry.BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (!activity.Source.Name.StartsWith(Constants.AspNetCore, StringComparison.OrdinalIgnoreCase))
        {
            base.OnEnd(activity);
            return;
        }

        var requestPath = activity.Tags.FirstOrDefault(t => t.Key == "http.route").Value;
        if ((requestPath?.EndsWith("/health", StringComparison.InvariantCultureIgnoreCase) ?? false)
         || (requestPath?.EndsWith("/health/deep", StringComparison.InvariantCultureIgnoreCase) ?? false))
        {
            activity.IsAllDataRequested = false;
            return;
        }
        base.OnEnd(activity);
    }
}
