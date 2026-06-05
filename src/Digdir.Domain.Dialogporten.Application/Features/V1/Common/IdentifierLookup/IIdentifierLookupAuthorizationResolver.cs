using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

internal interface IIdentifierLookupAuthorizationResolver
{
    Task<IdentifierLookupAuthorizationResolution> Resolve(
        IdentifierLookupDialogData dialogData,
        InstanceRef responseInstanceRef,
        List<AcceptedLanguage>? acceptedLanguages,
        CancellationToken cancellationToken);
}

internal sealed record IdentifierLookupAuthorizationResolution(
    bool HasAccess,
    IdentifierLookupAuthorizationEvidenceDto Evidence);
