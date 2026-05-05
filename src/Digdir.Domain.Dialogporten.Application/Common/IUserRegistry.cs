using System.Diagnostics;
using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Parties;
using UserIdType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogUserType.Values;

namespace Digdir.Domain.Dialogporten.Application.Common;

public interface IUserRegistry
{
    UserId GetCurrentUserId();
    Task<UserInformation> GetCurrentUserInformation(CancellationToken cancellationToken);
}

public sealed class UserId
{
    public required UserIdType Type { get; set; }
    public required string ExternalId { get; init; }
    public string ExternalIdWithPrefix => (Type switch
    {
        UserIdType.Person or UserIdType.ServiceOwnerOnBehalfOfPerson => NorwegianPersonIdentifier.PrefixWithSeparator,
        UserIdType.SystemUser => SystemUserIdentifier.PrefixWithSeparator,
        UserIdType.IdportenEmailIdentifiedUser => IdportenEmailUserIdentifier.PrefixWithSeparator,
        UserIdType.AltinnSelfIdentifiedUser => AltinnSelfIdentifiedUserIdentifier.PrefixWithSeparator,
        UserIdType.FeideUser => FeideUserIdentifier.PrefixWithSeparator,
        UserIdType.ServiceOwner => NorwegianOrganizationIdentifier.PrefixWithSeparator,
        UserIdType.Unknown => string.Empty,
        _ => throw new UnreachableException("Unknown UserIdType")
    }) + ExternalId;
}

public sealed class UserInformation
{
    public required UserId UserId { get; init; }
    public string? Name { get; init; }
}

public sealed class UserRegistry : IUserRegistry
{
    private readonly IUser _user;
    private readonly IPartyNameRegistry _partyNameRegistry;

    public UserRegistry(
        IUser user,
        IPartyNameRegistry partyNameRegistry)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(partyNameRegistry);

        _user = user;
        _partyNameRegistry = partyNameRegistry;
    }

    public UserId GetCurrentUserId()
    {
        var principal = _user.GetPrincipal();
        var (userType, externalId) = principal.GetUserType();
        if (userType == UserIdType.Unknown)
        {
            throw CreateUserExternalIdNotFoundException(principal);
        }

        return new() { Type = userType, ExternalId = externalId };
    }

    private static InvalidOperationException CreateUserExternalIdNotFoundException(ClaimsPrincipal principal)
    {
        var identities = principal.Identities.ToArray();
        var claims = principal.Claims.ToArray();
        var authenticationTypes = FormatDiagnosticValues(identities.Select(x => x.AuthenticationType));
        var claimTypes = FormatDiagnosticValues(claims.Select(x => x.Type));

        return new(
            "User external id not found. " +
            $"IsAuthenticated={principal.Identity?.IsAuthenticated.ToString() ?? "False"}, " +
            $"AuthenticatedIdentityCount={identities.Count(x => x.IsAuthenticated)}, " +
            $"AuthenticationTypes={authenticationTypes}, " +
            $"ClaimTypes={claimTypes}, " +
            $"ClaimCount={claims.Length}");
    }

    private static string FormatDiagnosticValues(IEnumerable<string?> values)
    {
        var formattedValues = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return formattedValues.Length == 0
            ? "<none>"
            : string.Join(", ", formattedValues);
    }

    public async Task<UserInformation> GetCurrentUserInformation(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var name = userId.Type switch
        {
            UserIdType.Person
                or UserIdType.ServiceOwnerOnBehalfOfPerson
                or UserIdType.AltinnSelfIdentifiedUser
                or UserIdType.IdportenEmailIdentifiedUser
                or UserIdType.FeideUser
                or UserIdType.SystemUser => await _partyNameRegistry.GetName(userId.ExternalIdWithPrefix, cancellationToken),
            UserIdType.Unknown => throw new UnreachableException(),
            UserIdType.ServiceOwner => throw new UnreachableException(),
            _ => throw new UnreachableException()
        };
        return new()
        {
            UserId = userId,
            Name = name
        };
    }
}

internal sealed class LocalDevelopmentUserRegistryDecorator : IUserRegistry
{
    private const string LocalDevelopmentUserName = "Local Development User";
    private readonly IUserRegistry _userRegistry;

    public LocalDevelopmentUserRegistryDecorator(IUserRegistry userRegistry)
    {
        ArgumentNullException.ThrowIfNull(userRegistry);

        _userRegistry = userRegistry;
    }

    public UserId GetCurrentUserId() => _userRegistry.GetCurrentUserId();

    public Task<UserInformation> GetCurrentUserInformation(CancellationToken cancellationToken)
        => Task.FromResult(new UserInformation
        {
            UserId = GetCurrentUserId(),
            Name = LocalDevelopmentUserName
        });
}
