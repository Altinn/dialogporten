namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

/// <summary>
/// Resolves authorization result and evidence for lookup responses.
/// </summary>
internal interface IIdentifierLookupAuthorizationResolver
{
    Task<IdentifierLookupAuthorizationResolution> Resolve(
        IdentifierLookupDialogData dialogData,
        InstanceRef requestRef,
        string responseInstanceRef,
        CancellationToken cancellationToken);
}

internal sealed record IdentifierLookupAuthorizationResolution(
    bool HasAccess,
    IdentifierLookupAuthorizationEvidenceDto Evidence);
