using System.Diagnostics;
using System.Text;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using Microsoft.Extensions.ObjectPool;

namespace Digdir.Domain.Dialogporten.GraphQL;

public class DialogportenGqlActivityEnricher(
    ObjectPool<StringBuilder> stringBuilderPool,
    InstrumentationOptions options) : ActivityEnricher(stringBuilderPool, options)
{
    private const string ActivityName = "gql.activity";
    private const string HttpRoute = "http.route";

    public override void EnrichExecuteRequest(IRequestContext context, Activity activity)
    {
        base.EnrichExecuteRequest(context, activity);
        var rootActivity = GetRootActivity(activity);
        rootActivity.SetTag(ActivityName, activity.DisplayName);
    }

    /// <summary>
    /// Renames the root activity's operation name
    ///
    /// Without this method, the name would be "/graphql/{**slug}" for any request, making it harder to differentiate various GQL operations in Application Insights.
    /// With this method, the name will be something like "GQL:query { dialogById }"
    ///
    /// This implementation is mostly copied from https://github.com/danhalvorsen/Energy/blob/db4c4e39f90b1f8f9cf074e20241695651cd3af3/src/Cch.MobileApi/Telemetry/CchTelemetryExtensions.cs#L105
    /// </summary>
    public static void RenameOperationName(Activity activity)
    {
        var activityName = activity.GetTagItem(ActivityName)?.ToString();
        if (activityName == null)
        {
            return;
        }

        activity.SetTag(HttpRoute, "GQL:" + activityName);
        activity.DisplayName = activityName;
    }

    private static Activity GetRootActivity(Activity activity)
    {
        var currentActivity = activity;

        while (currentActivity.Parent != null)
        {
            currentActivity = currentActivity.Parent;
        }

        return currentActivity;
    }
}
