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

    Task<string> ResolveOutputInstanceRef(
        InstanceRef requestRef,
        IdentifierLookupDialogData dialogData,
        CancellationToken cancellationToken);
}

internal sealed record IdentifierLookupDialogData(
    Guid DialogId,
    string Party,
    string Org,
    string ServiceResource,
    IReadOnlyList<string> ServiceOwnerLabels,
    IReadOnlyList<ResourceLocalization> Title,
    IReadOnlyList<ResourceLocalization>? NonSensitiveTitle);
