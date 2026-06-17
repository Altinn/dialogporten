using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Execution.Instrumentation;

namespace Digdir.Domain.Dialogporten.GraphQL;

/// <summary>
/// Restores operation-name-based naming of the GraphQL request activity in Application Insights.
///
/// In HotChocolate 16 the root span is owned by the transport instrumentation (ASP.NET Core), not
/// Hot Chocolate, and the previous mechanism (a custom <c>ActivityEnricher</c> /
/// <c>InstrumentationOptions.RenameRootActivity</c>) was removed. Without this, every GraphQL request
/// is named "/graphql/{**slug}", making it impossible to differentiate operations.
///
/// This listener captures the operation name during execution and stashes it on the root activity as
/// a custom property; <see cref="EnrichRootActivity"/> then applies it as the display name from the
/// ASP.NET Core instrumentation's EnrichWithHttpResponse callback.
///
/// See the v15 -> v16 migration guide:
/// https://chillicream.com/docs/hotchocolate/v16/migrating/migrate-from-15-to-16/
/// </summary>
public sealed class RenameRootActivityListener : ExecutionDiagnosticEventListener
{
    private const string DisplayNamePropertyName = "graphqlDisplayName";
    private const string HttpRoute = "http.route";
    private const string Prefix = "GQL: ";
    private const int PrefixLength = 5;
    private const int MaxLength = 100;

    public override IDisposable ExecuteRequest(RequestContext context) => new OperationNameScope(context);

    /// <summary>
    /// Applies the operation name captured during execution (if any) as the activity's display name,
    /// e.g. "GQL: query getDialogById".
    /// </summary>
    public static void EnrichRootActivity(Activity activity)
    {
        if (activity.GetCustomProperty(DisplayNamePropertyName) is not string rawName)
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

    private sealed class OperationNameScope(RequestContext context) : IDisposable
    {
        public void Dispose()
        {
            if (Activity.Current is not { } activity
                || !context.TryGetOperation(out var operation))
            {
                return;
            }

            var operationType = operation.Definition.Operation.ToString().ToLowerInvariant();

            // Name the activity by the root fields' schema names (e.g. "searchDialogs") rather than the
            // operation name, which is arbitrary user-supplied input we don't want in telemetry. Schema
            // field names are low-cardinality; sorting keeps the name stable regardless of selection order.
            var rootFieldNames = new List<string>();
            foreach (var selection in operation.RootSelectionSet.Selections)
            {
                var fieldName = selection.Field.Name;
                if (!rootFieldNames.Contains(fieldName))
                {
                    rootFieldNames.Add(fieldName);
                }
            }

            rootFieldNames.Sort(StringComparer.Ordinal);

            var displayName = rootFieldNames.Count > 0
                ? $"{operationType} {string.Join(", ", rootFieldNames)}"
                : operationType;

            var root = activity;
            while (root.Parent is { } parent)
            {
                root = parent;
            }

            root.SetCustomProperty(DisplayNamePropertyName, displayName);
        }
    }
}
