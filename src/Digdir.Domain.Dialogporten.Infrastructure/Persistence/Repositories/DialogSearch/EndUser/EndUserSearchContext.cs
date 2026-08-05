using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser;

internal sealed record EndUserSearchContext(
    GetDialogsQuery Query,
    DialogSearchAuthorizationResult AuthorizedResources)
{
    private int? _effectivePartyCount;
    private int? _effectiveServiceCount;

    // Cached: the selector calls Score() on every strategy (and the winner's BuildSql() reads these too),
    // so the authorization aggregates are computed at most once per request instead of O(strategies).
    public int EffectivePartyCount =>
        _effectivePartyCount ??= DialogEndUserSearchSqlHelpers.CountEffectiveParties(AuthorizedResources);

    public int EffectiveServiceCount =>
        _effectiveServiceCount ??= DialogEndUserSearchSqlHelpers.CountEffectiveServices(AuthorizedResources);
}
