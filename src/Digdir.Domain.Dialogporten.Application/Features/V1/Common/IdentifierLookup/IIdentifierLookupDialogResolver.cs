using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

internal enum IdentifierLookupDeletedDialogVisibility
{
    ExcludeDeleted = 0,
    IncludeDeleted = 1
}

/// <summary>
/// Resolves dialog data used by identifier lookup responses, keeping lookup-specific data access logic out of handlers.
/// </summary>
internal interface IIdentifierLookupDialogResolver
{
    Task<IdentifierLookupDialogData?> Resolve(
        InstanceRef instanceRef,
        IdentifierLookupDeletedDialogVisibility deletedDialogVisibility,
        CancellationToken cancellationToken);

    string ResolveOutputInstanceRef(InstanceRef requestRef, IdentifierLookupDialogData dialogData);
}

internal sealed record IdentifierLookupDialogData(
    Guid DialogId,
    string Party,
    string Org,
    string ServiceResource,
    List<string> ServiceOwnerLabels,
    List<ResourceLocalization> Title,
    List<ResourceLocalization>? NonSensitiveTitle);
