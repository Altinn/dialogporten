using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

internal interface IIdentifierLookupAuthorizationResolver
{
    Task<IdentifierLookupAuthorizationResolution> Resolve(
        IdentifierLookupDialogData dialogData,
        InstanceRef requestRef,
        InstanceRef responseInstanceRef,
        CancellationToken cancellationToken);
}

internal sealed record IdentifierLookupAuthorizationResolution(
    bool HasAccess,
    IdentifierLookupAuthorizationEvidenceDto Evidence);
