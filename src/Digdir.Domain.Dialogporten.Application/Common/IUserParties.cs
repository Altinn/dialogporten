using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Common;

public interface IUserParties
{
    Task<AuthorizedPartiesResult> GetUserParties(CancellationToken cancellationToken = default);
}

public sealed class UserParties : IUserParties
{
    private readonly IUser _user;
    private readonly IAltinnAuthorization _altinnAuthorization;
    private readonly ILogger<UserParties> _logger;

    public UserParties(IUser user, IAltinnAuthorization altinnAuthorization, ILogger<UserParties> logger)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(altinnAuthorization);
        ArgumentNullException.ThrowIfNull(logger);

        _user = user;
        _altinnAuthorization = altinnAuthorization;
        _logger = logger;
    }

    public Task<AuthorizedPartiesResult> GetUserParties(CancellationToken cancellationToken = default)
    {
        var userPrincipal = _user.GetPrincipal();
        var partyIdentifier = userPrincipal.GetEndUserPartyIdentifier();

        if (partyIdentifier is null)
        {
            var (userType, _) = userPrincipal.GetUserType();
            _logger.LogError(
                "The request was authenticated, but could not find party identifier. UserType={UserType}, {DiagnosticSummary}",
                userType,
                userPrincipal.GetDiagnosticSummary());
            throw new UnreachableException("Party identifier could not be found");
        }

        return _altinnAuthorization.GetAuthorizedParties(partyIdentifier, cancellationToken: cancellationToken);
    }
}
