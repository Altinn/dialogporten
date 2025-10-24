using System.Diagnostics;
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
        UserIdType.SelfIdentifiedUser => AltinnUserIdentifier.PrefixWithSeparator,
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
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _partyNameRegistry = partyNameRegistry ?? throw new ArgumentNullException(nameof(partyNameRegistry));
    }

    public UserId GetCurrentUserId()
    {
        var (userType, externalId) = _user.GetPrincipal().GetUserType();
        if (userType == UserIdType.Unknown)
        {
            throw new InvalidOperationException("User external id not found");
        }

        return new() { Type = userType, ExternalId = externalId };
    }

    public async Task<UserInformation> GetCurrentUserInformation(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var name = userId.Type switch
        {
            UserIdType.Person or UserIdType.ServiceOwnerOnBehalfOfPerson => await _partyNameRegistry.GetName(userId.ExternalIdWithPrefix, cancellationToken),
            UserIdType.SystemUser => "System User",// TODO: Implement when SystemUsers are introduced?
            UserIdType.SelfIdentifiedUser => _user.GetPrincipal().TryGetClaimValue("urn:altinn:username", out var username) ? username : "Self-Identified User",
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
        _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
    }

    public UserId GetCurrentUserId() => _userRegistry.GetCurrentUserId();

    public Task<UserInformation> GetCurrentUserInformation(CancellationToken cancellationToken)
        => Task.FromResult(new UserInformation
        {
            UserId = GetCurrentUserId(),
            Name = LocalDevelopmentUserName
        });
}
