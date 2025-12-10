using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Application.Common;

public interface IUserResourceRegistry
{
    Task<bool> CurrentUserIsOwner(string serviceResource, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetCurrentUserResourceIds(CancellationToken cancellationToken);
    bool UserCanModifyResourceType(string serviceResourceType);
    bool IsCurrentUserServiceOwnerAdmin();
    Task<string> GetCurrentUserOrgShortName(CancellationToken cancellationToken);
}

internal sealed class UserResourceRegistry : IUserResourceRegistry
{
    private readonly IUser _user;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly ILogger<UserResourceRegistry> _logger;

    public UserResourceRegistry(IUser user, IResourceRegistry resourceRegistry, ILogger<UserResourceRegistry> logger)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CurrentUserIsOwner(string serviceResource, CancellationToken cancellationToken)
    {
        var resourceIds = await GetCurrentUserResourceIds(cancellationToken);
        return resourceIds.Contains(serviceResource);
    }

    public async Task<IReadOnlyCollection<string>> GetCurrentUserResourceIds(CancellationToken cancellationToken)
    {
        if (!_user.GetPrincipal().TryGetConsumerOrgNumber(out var orgNumber))
        {
            throw new UnreachableException();
        }

        var dic = await _resourceRegistry.GetResourceInformationForOrg(orgNumber, cancellationToken);
        return dic.Select(x => x.ResourceId).ToList();
    }

    public async Task<string> GetCurrentUserOrgShortName(CancellationToken cancellationToken)
    {
        if (_user.GetPrincipal().TryGetOrganizationShortName(out var shortName))
        {
            return shortName;
        }

        if (!_user.GetPrincipal().TryGetConsumerOrgNumber(out var orgNumber))
        {
            throw new UnreachableException();
        }

        var dic = await _resourceRegistry.GetResourceInformationForOrg(orgNumber, cancellationToken);

        var orgShortNames = dic
            .Select(x => x.OwnOrgShortName)
            .Distinct()
            .ToArray();

        if (orgShortNames.Length > 1)
        {
            _logger.LogWarning("More than one short name found for org number {OrgNumber}: {ShortNames}", orgNumber, string.Join(", ", orgShortNames));
        }

        // Each organization number should only have one short name in RR
        var name = orgShortNames.FirstOrDefault();

        return string.IsNullOrWhiteSpace(name)
            ? throw new InvalidOperationException("Could not find organization short name for org number " + orgNumber)
            : name;
    }

    public bool UserCanModifyResourceType(string serviceResourceType) => serviceResourceType switch
    {
        ResourceRegistry.Constants.CorrespondenceService => _user.GetPrincipal().HasScope(AuthorizationScope.CorrespondenceScope),
        null => false,
        _ => true
    };

    public bool IsCurrentUserServiceOwnerAdmin() => _user.GetPrincipal().HasScope(AuthorizationScope.ServiceOwnerAdminScope);
}

internal sealed class LocalDevelopmentUserResourceRegistryDecorator : IUserResourceRegistry
{
    private readonly IUserResourceRegistry _userResourceRegistry;

    public LocalDevelopmentUserResourceRegistryDecorator(IUserResourceRegistry userResourceRegistry)
    {
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
    }

    public Task<bool> CurrentUserIsOwner(string serviceResource, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task<IReadOnlyCollection<string>> GetCurrentUserResourceIds(CancellationToken cancellationToken) =>
        _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);

    public bool UserCanModifyResourceType(string serviceResourceType) => true;
    public bool IsCurrentUserServiceOwnerAdmin() => true;

    public Task<string> GetCurrentUserOrgShortName(CancellationToken cancellationToken) =>
        _userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
}
