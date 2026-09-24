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
        ArgumentNullException.ThrowIfNull(userResourceRegistry);
        ArgumentNullException.ThrowIfNull(resourceRegistry);
        ArgumentNullException.ThrowIfNull(domainContext);

        _userResourceRegistry = userResourceRegistry;
        _resourceRegistry = resourceRegistry;
        _domainContext = domainContext;
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

        var appReferences = GetAdditionalResourceAppReferences(dialog).ToList();
        if (appReferences.Count != 0)
        {
            var currentUserOrg = await _userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
            var notOwnedApps = appReferences
                .Where(x => !string.Equals(x.Org, currentUserOrg, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Reference)
                .ToList();

            if (notOwnedApps.Count != 0)
            {
                return new Forbidden($"Not allowed to reference the following unowned apps: [{string.Join(", ", notOwnedApps)}].");
            }
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
                 Service resource '{dialog.ServiceResource}' is invalid due to one or more of the following reasons:
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
            .Concat(dialog.ApiActions.Select(action => action.AuthorizationAttribute))
            .Concat(dialog.GuiActions.Select(action => action.AuthorizationAttribute))
            .Concat(dialog.Transmissions.Select(transmission => transmission.AuthorizationAttribute))
            .Concat(GetAuthorizationContexts(dialog).SelectMany(context => new[]
            {
                context.ServiceResource,
                // Defense-in-depth: validation forbids resource references here, but the prefix
                // filter below makes sweeping this field free.
                context.AdditionalResourceAttribute
            }))
            .Select(x => x?.ToLowerInvariant())
            .Distinct()
            .Where(IsPrimaryResource)
            .Cast<string>();

    private static IEnumerable<Domain.Dialogs.Entities.AuthorizationContexts.AuthorizationContext> GetAuthorizationContexts(DialogEntity dialog) =>
        Enumerable.Empty<Domain.Dialogs.Entities.AuthorizationContexts.AuthorizationContext?>()
            .Concat(dialog.ApiActions.Select(x => x.AuthorizationContext))
            .Concat(dialog.GuiActions.Select(x => x.AuthorizationContext))
            .Concat(dialog.Transmissions.Select(x => x.AuthorizationContext))
            .Concat(dialog.Attachments.Select(x => x.AuthorizationContext))
            .Concat(dialog.Transmissions.SelectMany(x => x.Attachments).Select(x => x.AuthorizationContext))
            .Concat(dialog.Transmissions.SelectMany(x => x.NavigationalActions).Select(x => x.AuthorizationContext))
            .OfType<Domain.Dialogs.Entities.AuthorizationContexts.AuthorizationContext>();

    private static bool IsPrimaryResource(string? resource) =>
        resource is not null
        && resource.StartsWith(Domain.Common.Constants.ServiceResourcePrefix, StringComparison.OrdinalIgnoreCase);

    // Defense-in-depth, mirroring the resource-prefix sweep above: AuthorizationContextDtoValidator already
    // rejects the app namespace (and any value that would implicitly expand into an app identity) outright,
    // so nothing should reach this in practice. Apps aren't resource-registry entries, so ownership is
    // org-based rather than an id lookup; a reference whose org can't be parsed out (a malformed
    // 'app_{org}_{appId}' value) is treated as not owned.
    private static IEnumerable<(string Reference, string? Org)> GetAdditionalResourceAppReferences(DialogEntity dialog) =>
        GetAuthorizationContexts(dialog)
            .Select(context => context.AdditionalResourceAttribute)
            .Where(x => x is not null)
            .Select(x => x!)
            .Where(x => x.StartsWith(Domain.Common.Constants.AppResourcePrefix, StringComparison.OrdinalIgnoreCase))
            .Select(x => (Reference: x, Org: ExtractAppOrg(x)));

    private static string? ExtractAppOrg(string value)
    {
        var lastColonIndex = value.LastIndexOf(':');
        var tail = lastColonIndex == -1 ? value : value[(lastColonIndex + 1)..];
        if (!tail.StartsWith(Domain.Common.Constants.AppResourceIdPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var parts = tail.Split('_');
        return parts.Length >= 3 ? parts[1] : null;
    }
}
