using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.Application.Common;

public interface IUserParties
{
    Task<AuthorizedPartiesResult> GetUserParties(CancellationToken cancellationToken = default);
}

public sealed class UserParties : IUserParties
{
    private readonly IUser _user;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public UserParties(IUser user, IAltinnAuthorization altinnAuthorization)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
    }

    public Task<AuthorizedPartiesResult> GetUserParties(CancellationToken cancellationToken = default)
    {
        var partyIdentifier = _user.GetPrincipal().GetEndUserPartyIdentifier();
        return partyIdentifier != null
            ? _altinnAuthorization.GetAuthorizedParties(partyIdentifier, cancellationToken: cancellationToken)
            : Task.FromResult(new AuthorizedPartiesResult());
    }
}
