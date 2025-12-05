using System.Diagnostics;

namespace Digdir.Library.Utils.AspNet;

public class GraphQLFilter : OpenTelemetry.BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        if (!activity.Source.Name.StartsWith(Constants.HotChocolateDiagnostics, StringComparison.OrdinalIgnoreCase))
        {
            base.OnEnd(activity);
            return;
        }

        if (activity.Tags.HasFilteredGraphQLTarget())
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }

        base.OnEnd(activity);
    }
}

internal static class GraphQLActivityTagsExtensions
{
    public static bool HasFilteredGraphQLTarget(this IEnumerable<KeyValuePair<string, string?>> tags) =>
        tags.Any(t => t is
        {
            Key: Constants.GraphQLTarget,
            Value: Constants.GraphQLTargetParseHttpRequest or
                   Constants.GraphQLTargetValidateDocument or
                   Constants.GraphQLTargetCompileOperation or
                   Constants.GraphQLTargetFormatHttpResponse
        });
}
