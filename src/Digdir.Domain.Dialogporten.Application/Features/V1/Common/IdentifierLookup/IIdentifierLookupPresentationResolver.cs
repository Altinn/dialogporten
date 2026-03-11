using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup;

internal interface IIdentifierLookupPresentationResolver
{
    Task<(IdentifierLookupServiceResourceDto ServiceResource, IdentifierLookupServiceOwnerDto ServiceOwner)> Resolve(
        string serviceResource,
        string orgCode,
        List<AcceptedLanguage>? acceptedLanguages,
        CancellationToken cancellationToken);
}
