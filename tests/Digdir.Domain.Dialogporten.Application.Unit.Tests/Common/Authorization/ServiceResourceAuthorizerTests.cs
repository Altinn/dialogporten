using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using NSubstitute;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common.Authorization;

public class ServiceResourceAuthorizerTests
{
    private const string OwnedResource = "urn:altinn:resource:owned-service";
    private const string UnownedResource = "urn:altinn:resource:unowned-service";

    [Fact]
    public async Task AuthorizeServiceResources_Should_Allow_Owned_Context_Resources()
    {
        var dialog = CreateDialog();
        dialog.GuiActions.Add(new DialogGuiAction
        {
            AuthorizationContext = new DialogGuiActionAuthorizationContext
            {
                Action = "read",
                ServiceResource = OwnedResource
            }
        });

        var result = await CreateSut().AuthorizeServiceResources(dialog, CancellationToken.None);

        Assert.IsType<Success>(result.Value);
    }

    public static TheoryData<Action<DialogEntity, AuthorizationContext>> ContextCarriers => new()
    {
        (dialog, context) => dialog.ApiActions.Add(new DialogApiAction { AuthorizationContext = AsContext<DialogApiActionAuthorizationContext>(context) }),
        (dialog, context) => dialog.GuiActions.Add(new DialogGuiAction { AuthorizationContext = AsContext<DialogGuiActionAuthorizationContext>(context) }),
        (dialog, context) => dialog.Transmissions.Add(new DialogTransmission { AuthorizationContext = AsContext<DialogTransmissionAuthorizationContext>(context) }),
        (dialog, context) => dialog.Attachments.Add(new DialogAttachment { AuthorizationContext = AsContext<AttachmentAuthorizationContext>(context) }),
        (dialog, context) => dialog.Transmissions.Add(new DialogTransmission
        {
            Attachments = [new DialogTransmissionAttachment { AuthorizationContext = AsContext<AttachmentAuthorizationContext>(context) }]
        }),
        (dialog, context) => dialog.Transmissions.Add(new DialogTransmission
        {
            NavigationalActions = [new DialogTransmissionNavigationalAction { AuthorizationContext = AsContext<DialogTransmissionNavigationalActionAuthorizationContext>(context) }]
        })
    };

    [Theory]
    [MemberData(nameof(ContextCarriers))]
    public async Task AuthorizeServiceResources_Should_Forbid_Unowned_Context_ServiceResource_On_Any_Carrier(
        Action<DialogEntity, AuthorizationContext> addCarrier)
    {
        var dialog = CreateDialog();
        addCarrier(dialog, new DialogGuiActionAuthorizationContext { ServiceResource = UnownedResource });

        var result = await CreateSut().AuthorizeServiceResources(dialog, CancellationToken.None);

        var forbidden = Assert.IsType<Forbidden>(result.Value);
        Assert.Contains(UnownedResource, forbidden.Reasons.Single());
    }

    [Fact]
    public async Task AuthorizeServiceResources_Should_Forbid_Resource_Reference_Smuggled_Into_AdditionalResourceAttribute()
    {
        // Write-side validation forbids resource references in additionalResourceAttribute, but the
        // ownership sweep must cover it as defense in depth.
        var dialog = CreateDialog();
        dialog.Transmissions.Add(new DialogTransmission
        {
            AuthorizationContext = new DialogTransmissionAuthorizationContext
            {
                AdditionalResourceAttribute = UnownedResource
            }
        });

        var result = await CreateSut().AuthorizeServiceResources(dialog, CancellationToken.None);

        Assert.IsType<Forbidden>(result.Value);
    }

    [Fact]
    public async Task AuthorizeServiceResources_Should_Forbid_Unowned_Legacy_AuthorizationAttribute()
    {
        var dialog = CreateDialog();
        dialog.ApiActions.Add(new DialogApiAction { Action = "read", AuthorizationAttribute = UnownedResource });

        var result = await CreateSut().AuthorizeServiceResources(dialog, CancellationToken.None);

        Assert.IsType<Forbidden>(result.Value);
    }

    private static TContext AsContext<TContext>(AuthorizationContext source)
        where TContext : AuthorizationContext, new() => new()
        {
            ServiceResource = source.ServiceResource,
            AdditionalResourceAttribute = source.AdditionalResourceAttribute,
            Parties = source.Parties,
            IncludeDialogParty = source.IncludeDialogParty,
            Action = source.Action
        };

    private static DialogEntity CreateDialog() => new()
    {
        ServiceResource = OwnedResource,
        ServiceResourceType = "GenericAccessResource"
    };

    private static ServiceResourceAuthorizer CreateSut()
    {
        var userResourceRegistry = Substitute.For<IUserResourceRegistry>();
        userResourceRegistry.IsCurrentUserServiceOwnerAdmin().Returns(false);
        userResourceRegistry.GetCurrentUserResourceIds(Arg.Any<CancellationToken>())
            .Returns([OwnedResource]);
        userResourceRegistry.UserCanModifyResourceType(Arg.Any<string>()).Returns(true);

        return new ServiceResourceAuthorizer(
            userResourceRegistry,
            Substitute.For<IResourceRegistry>(),
            Substitute.For<IDomainContext>());
    }
}
