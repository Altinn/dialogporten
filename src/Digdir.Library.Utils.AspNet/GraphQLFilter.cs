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

        // Filter out internal GraphQL processing activities that don't add value to traces
        // We filter based on OperationName which identifies the internal processing step
        // Keep: ExecuteHttpRequest (main operation), ResolveFieldValue (field resolution)
        // Filter: ParseHttpRequest, ValidateDocument, CompileOperation, FormatHttpResponse
        if (activity.OperationName is
            Constants.GraphQLOperationParseHttpRequest or
            Constants.GraphQLOperationValidateDocument or
            Constants.GraphQLOperationCompileOperation or
            Constants.GraphQLOperationFormatHttpResponse)
        {
            activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
            return;
        }

        base.OnEnd(activity);
    }
}
