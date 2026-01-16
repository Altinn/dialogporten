using System.Diagnostics;
using System.Text;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using Microsoft.Extensions.ObjectPool;

namespace Digdir.Domain.Dialogporten.GraphQL;

public sealed class DialogportenGqlActivityEnricher(
    ObjectPool<StringBuilder> stringBuilderPool,
    InstrumentationOptions options) : ActivityEnricher(stringBuilderPool, options)
{
    private const string ActivityName = "gql.activity";
    private const string HttpRoute = "http.route";
    private const string Prefix = "GQL: ";
    private const int PrefixLength = 5;
    private const int MaxLength = 100;

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
    /// With this method, the name will be something like "GQL: query { dialogById }"
    ///
    /// This implementation is mostly copied from https://github.com/danhalvorsen/Energy/blob/db4c4e39f90b1f8f9cf074e20241695651cd3af3/src/Cch.MobileApi/Telemetry/CchTelemetryExtensions.cs#L105
    /// </summary>
    public static void RenameOperationName(Activity activity)
    {
        var tagValue = activity.GetTagItem(ActivityName);
        var rawName = tagValue as string ?? tagValue?.ToString();

        if (rawName == null)
        {
            return;
        }

        // Sanitize, truncate and add a prefix. Avoid allocations as this is a fairly hot path.
        var inputLimit = Math.Min(rawName.Length, MaxLength);
        var finalName = string.Create(PrefixLength + inputLimit, (rawName, inputLimit), (span, state) =>
        {
            Prefix.AsSpan().CopyTo(span);
            var targetSlice = span[PrefixLength..];
            var sourceSlice = state.rawName.AsSpan(0, state.inputLimit);
            for (var i = 0; i < sourceSlice.Length; i++)
            {
                var c = sourceSlice[i];
                targetSlice[i] = (c is '\r' or '\n') ? ' ' : c;
            }
        });

        activity.SetTag(HttpRoute, finalName);
        activity.DisplayName = finalName;
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
