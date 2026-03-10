using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

internal enum IdentifierLookupDeletedDialogVisibility
{
    ExcludeDeleted = 0,
    IncludeDeleted = 1
}

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
