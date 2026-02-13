using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal sealed record EndUserSearchContext(
    GetDialogsQuery Query,
    DialogSearchAuthorizationResult AuthorizedResources);
