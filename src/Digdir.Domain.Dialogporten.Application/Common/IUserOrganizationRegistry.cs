using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;

namespace Digdir.Domain.Dialogporten.Application.Common;

public interface IUserOrganizationRegistry
{
    Task<string?> GetCurrentUserOrgShortName(CancellationToken cancellationToken);
}

public sealed class UserOrganizationRegistry : IUserOrganizationRegistry
{
    private readonly IUser _user;
    private readonly IServiceOwnerNameRegistry _serviceOwnerNameRegistry;

    public UserOrganizationRegistry(IUser user, IServiceOwnerNameRegistry serviceOwnerNameRegistry)
    {
        _user = user ?? throw new ArgumentNullException(nameof(user));
        _serviceOwnerNameRegistry = serviceOwnerNameRegistry ?? throw new ArgumentNullException(nameof(serviceOwnerNameRegistry));
    }

    public async Task<string?> GetCurrentUserOrgShortName(CancellationToken cancellationToken)
    {
        if (!_user.GetPrincipal().TryGetConsumerOrgNumber(out var orgNumber))
        {
            return null;
        }

        var orgInfo = await _serviceOwnerNameRegistry.GetServiceOwnerInfo(orgNumber, cancellationToken);

        return orgInfo?.ShortName;
    }

}

internal sealed class LocalDevelopmentUserOrganizationRegistryDecorator : IUserOrganizationRegistry
{
    public LocalDevelopmentUserOrganizationRegistryDecorator(IUserOrganizationRegistry _) { }

    public Task<string?> GetCurrentUserOrgShortName(CancellationToken cancellationToken) => Task.FromResult("digdir")!;
}
