using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

public interface IServiceResourceAuthorizer
{
    Task<AuthorizeServiceResourcesResult> AuthorizeServiceResources(
        DialogEntity dialog,
        CancellationToken cancellationToken);

    Task<SetResourceTypeResult> SetResourceType(
        DialogEntity dialog,
        CancellationToken cancellationToken);
}

[GenerateOneOf]
public sealed partial class AuthorizeServiceResourcesResult : OneOfBase<Success, Forbidden>;

[GenerateOneOf]
public sealed partial class SetResourceTypeResult : OneOfBase<Success, DomainContextInvalidated>;

public struct DomainContextInvalidated;

internal sealed class ServiceResourceAuthorizer : IServiceResourceAuthorizer
{
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IDomainContext _domainContext;

    public ServiceResourceAuthorizer(
        IUserResourceRegistry userResourceRegistry,
        IResourceRegistry resourceRegistry,
        IDomainContext domainContext)
    {
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
    }

    public async Task<AuthorizeServiceResourcesResult> AuthorizeServiceResources(
        DialogEntity dialog,
        CancellationToken cancellationToken)
    {
        if (_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            return new Success();
        }

        var ownedResources = await _userResourceRegistry.GetCurrentUserResourceIds(cancellationToken);
        var notOwnedResources = GetPrimaryServiceResourceReferences(dialog)
            .Except(ownedResources)
            .ToList();

        if (notOwnedResources.Count != 0)
        {
            return new Forbidden($"Not allowed to reference the following unowned resources: [{string.Join(", ", notOwnedResources)}].");
        }

        if (!_userResourceRegistry.UserCanModifyResourceType(dialog.ServiceResourceType))
        {
            return new Forbidden($"User cannot create or modify a dialog with resource type {dialog.ServiceResourceType}.");
        }

        return new Success();
    }

    public async Task<SetResourceTypeResult> SetResourceType(DialogEntity dialog, CancellationToken cancellationToken)
    {
        var serviceResourceInformation = await _resourceRegistry.GetResourceInformation(dialog.ServiceResource, cancellationToken);
        if (serviceResourceInformation is null)
        {
            _domainContext.AddError(nameof(CreateDialogDto.ServiceResource),
                $"""
                 Service resource '{dialog.ServiceResource}' not found or unsupported due to one or more of the following reasons:
                 - It does not exist in the resource registry.
                 - It does not have mandatory fields 'CompetentAuthority.Organization' / 'CompetentAuthority.OrgCode' set.
                 - It is not of the following supported resource types: [{string.Join(", ", Constants.SupportedResourceTypes)}].
                 """);
            return new DomainContextInvalidated();
        }

        dialog.ServiceResourceType = serviceResourceInformation.ResourceType;
        return new Success();
    }

    private static IEnumerable<string> GetPrimaryServiceResourceReferences(DialogEntity dialog) =>
        Enumerable.Empty<string?>()
            .Append(dialog.ServiceResource)
            .Concat(dialog.ApiActions.Select(action => action.AuthorizationAttribute!))
            .Concat(dialog.GuiActions.Select(action => action.AuthorizationAttribute!))
            .Concat(dialog.Transmissions.Select(transmission => transmission.AuthorizationAttribute!))
            .Select(x => x?.ToLowerInvariant())
            .Distinct()
            .Where(IsPrimaryResource)
            .Cast<string>();

    private static bool IsPrimaryResource(string? resource) =>
        resource is not null
        && resource.StartsWith(Domain.Common.Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase);
}
