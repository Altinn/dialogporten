using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Should_Return_Dialog_With_Correct_Id()
    {
        const string externalReference = "Bare for å være sikker...";
        var id = NewUuidV7();
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog((x, _) => (x.Dto.Id, x.Dto.ExternalReference) = (id, externalReference))
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .SendCommand(_ => new GetDialogQuery { DialogId = id })
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Id.Should().Be(id);
                x.ExternalReference.Should().Be(externalReference);
            });
    }

    [Fact]
    public async Task Get_ReturnsSimpleDialog_WhenDialogExists()
    {
        CreateDialogDto createDto = null!;

        await FlowBuilder.For(Application)
            .CreateComplexDialog((x, _) => createDto = x.Dto)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                var mappedStatus = Application.GetMapper()
                    .Map<DialogStatus.Values>(createDto.Status);
                result.Status.Should().Be(mappedStatus);

                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(createDto, options => options
                    .Excluding(x => x.UpdatedAt)
                    .Excluding(x => x.CreatedAt)
                    .Excluding(x => x.SystemLabel)
                    .Excluding(x => x.Status)
                );
            });
    }

    [Fact]
    public Task Get_Dialog_Should_Not_Mask_Expired_Attachment_Urls() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.Now.AddDays(1));
                x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.Now.AddDays(1));

                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
            })
            .OverrideUtc(TimeSpan.FromDays(2))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Transmissions.Should().NotBeEmpty()
                    .And.AllSatisfy(t => t.Attachments.Should().NotBeEmpty()
                        .And.AllSatisfy(a => a.Urls.Should().NotBeEmpty()
                            .And.AllSatisfy(url => url.Url.Should().NotBeNull()
                                .And.NotBe(Constants.ExpiredUri))));

                x.Attachments.Should().NotBeEmpty()
                    .And.AllSatisfy(a => a.Urls.Should().NotBeEmpty()
                        .And.AllSatisfy(url => url.Url.Should().NotBeNull()
                            .And.NotBe(Constants.ExpiredUri)));
            });

    private const string DialogAttachmentName = "dialog-attachment";
    private const string TransmissionAttachmentName = "transmission-attachment";

    [Fact]
    public Task Get_Dialog_Should_Return_Attachment_Names() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddAttachment(attachment =>
                    attachment.Name = DialogAttachmentName);
                x.AddTransmission(transmission =>
                    transmission.AddAttachment(attachment =>
                        attachment.Name = TransmissionAttachmentName));
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Attachments.Should()
                    .ContainSingle(attachment =>
                        attachment.Name == DialogAttachmentName);
                x.Transmissions.Should().ContainSingle()
                    .Which.Attachments.Should()
                    .ContainSingle(attachment =>
                        attachment.Name == TransmissionAttachmentName);
            });

    [Fact]
    public async Task Get_ReturnsDialog_WhenDialogExists()
    {
        CreateDialogDto createDto = null!;

        await FlowBuilder.For(Application)
            .CreateComplexDialog((x, _) => createDto = x.Dto)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                var mappedStatus = Application.GetMapper()
                    .Map<DialogStatus.Values>(createDto.Status);
                result.Status.Should().Be(mappedStatus);

                result.Should().NotBeNull();
                result.Should().BeEquivalentTo(createDto, options => options
                    .Excluding(x => x.UpdatedAt)
                    .Excluding(x => x.CreatedAt)
                    .Excluding(x => x.SystemLabel)
                    .Excluding(x => x.Status));
            });
    }

    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public Task Get_Should_Populate_Obsolete_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.SystemLabel.Should()
                    .Be(SystemLabel.Values.Default));

    [Fact]
    public Task Get_Dialog_with_malformed_EndUserId() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SearchServiceOwnerDialogs(x =>
            {
                x.ServiceResource = ["urn:altinn:resource:super-simple-service"];
                x.EndUserId =
                    "urn:altinn:person:identifier-no:05848297888";
            })
            .ExecuteAndAssert<ValidationError>(result =>
            {
                result.Errors.Should().ContainSingle()
                    .Which.ErrorMessage.Should()
                    .Contain("EndUserId must be a valid end user identifier.");
            });

    [Fact]
    public Task Get_Dialog_Should_Not_Decorate_Authorization() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddApiAction();
                x.AddGuiAction();
                x.AddTransmission();
                x.AddMainContentReference();
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.ApiActions.Count.Should().NotBe(0);
                x.ApiActions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeNull());
                x.GuiActions.Count.Should().NotBe(0);
                x.GuiActions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeNull());
                x.Content.Should().NotBeNull();
                x.Content.MainContentReference.Should().NotBeNull();
                x.Content.MainContentReference.IsAuthorized.Should().BeNull();
                x.Transmissions.Count.Should().NotBe(0);
                x.Transmissions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeNull());
            });

    [Fact]
    public Task Get_Dialog_Should_Decorate_Authorization_True_When_EndUserId_Is_Supplied_And_Has_Main_Content_Access() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.AddApiAction();
                x.AddGuiAction();
                x.AddTransmission();
                x.AddMainContentReference();
            })
            .GetServiceOwnerDialogAsEndUser()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.ApiActions.Count.Should().NotBe(0);
                x.ApiActions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeTrue());
                x.GuiActions.Count.Should().NotBe(0);
                x.GuiActions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeTrue());
                x.Content.Should().NotBeNull();
                x.Content.MainContentReference.Should().NotBeNull();
                x.Content.MainContentReference.IsAuthorized.Should().BeTrue();
                x.Transmissions.Count.Should().NotBe(0);
                x.Transmissions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeTrue());
            });

    [Fact]
    public Task Get_Dialog_Should_Decorate_Authorization_True_When_EndUserId_Is_Supplied_And_Has_Specific_Access() =>
        FlowBuilder.For(Application, services =>
            {
                var authorizationResult = new DialogDetailsAuthorizationResult
                {
                    AuthorizedAltinnActions = [
                        new AltinnAction(Constants.ReadAction),
                        new AltinnAction("ApiAction", "urn:altinn:resource:api-action"),
                        new AltinnAction("GuiAction", "urn:altinn:resource:gui-action"),
                        new AltinnAction(Constants.TransmissionReadAction, "urn:altinn:resource:transmission-1"),
                        new AltinnAction(Constants.ReadAction, "urn:altinn:resource:transmission-2"),
                    ]
                };
                services.ConfigureDialogDetailsAuthorizationResult(authorizationResult);
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.AddApiAction(apiAction =>
                {
                    apiAction.Action = "ApiAction";
                    apiAction.AuthorizationAttribute = "urn:altinn:resource:api-action";
                });
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = "GuiAction";
                    guiAction.AuthorizationAttribute = "urn:altinn:resource:gui-action";
                });
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = "urn:altinn:resource:transmission-1";
                });
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = "urn:altinn:resource:transmission-2";
                });
                x.AddMainContentReference();
            })
            .GetServiceOwnerDialogAsEndUser()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.ApiActions.Count.Should().NotBe(0);
                x.ApiActions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeTrue());
                x.GuiActions.Count.Should().NotBe(0);
                x.GuiActions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeTrue());
                x.Content.Should().NotBeNull();
                x.Content.MainContentReference.Should().NotBeNull();
                x.Content.MainContentReference.IsAuthorized.Should().BeTrue();
                x.Transmissions.Count.Should().NotBe(0);
                x.Transmissions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeTrue());
            });

    [Fact]
    public Task Get_Dialog_Should_Decorate_Authorization_False_When_EndUserId_Is_Supplied_And_No_Specific_Access() =>
        FlowBuilder.For(Application, services =>
            {
                var authorizationResult = new DialogDetailsAuthorizationResult
                {
                    AuthorizedAltinnActions = [
                        new AltinnAction("subscribe"),
                        new AltinnAction("ApiAction", "urn:altinn:resource:restricted"),
                        new AltinnAction("GuiAction", "urn:altinn:resource:restricted"),
                        new AltinnAction(Constants.TransmissionReadAction, "urn:altinn:resource:restricted"),
                        new AltinnAction(Constants.ReadAction, "urn:altinn:resource:restricted"),
                    ]
                };
                services.ConfigureDialogDetailsAuthorizationResult(authorizationResult);
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.AddApiAction(apiAction =>
                {
                    apiAction.Action = "ApiAction";
                    apiAction.AuthorizationAttribute = "urn:altinn:resource:api-action";
                });
                x.AddGuiAction(guiAction =>
                {
                    guiAction.Action = "GuiAction";
                    guiAction.AuthorizationAttribute = "urn:altinn:resource:gui-action";
                });
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = "urn:altinn:resource:transmission-1";
                });
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = "urn:altinn:resource:transmission-2";
                });
                x.AddMainContentReference();
            })
            .GetServiceOwnerDialogAsEndUser()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.ApiActions.Count.Should().NotBe(0);
                x.ApiActions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeFalse());
                x.GuiActions.Count.Should().NotBe(0);
                x.GuiActions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeFalse());
                x.Content.Should().NotBeNull();
                x.Content.MainContentReference.Should().NotBeNull();
                x.Content.MainContentReference.IsAuthorized.Should().BeFalse();
                x.Transmissions.Count.Should().NotBe(0);
                x.Transmissions.Should().AllSatisfy(a => a.IsAuthorized.Should().BeFalse());
            });
}
