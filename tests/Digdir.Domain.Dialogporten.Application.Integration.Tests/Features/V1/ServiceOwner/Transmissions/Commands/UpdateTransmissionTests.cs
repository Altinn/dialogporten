using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Security.Claims;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using ContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ContentDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateTransmissionTests : ApplicationCollectionFixture
{
    private const string TransmissionIdKey = nameof(TransmissionIdKey);
    private const string ContentUpdatedAtKey = nameof(ContentUpdatedAtKey);

    public UpdateTransmissionTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Cannot_Use_Existing_Attachment_Id_In_Update()
    {
        var existingAttachmentId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.AddAttachment(x => x.Id = existingAttachmentId)))
            .AssertSuccessAndUpdateDialog(x =>
                x.AddTransmission(x =>
                    x.AddAttachment(x => x.Id = existingAttachmentId)))
            .ExecuteAndAssert<DomainError>(error =>
                error.ShouldHaveErrorWithText(existingAttachmentId.ToString()));
    }

    [Fact]
    public Task Can_Create_Simple_Transmission_In_Update() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                var transmission = UpdateDialogDialogTransmissionDto();
                x.Dto.Transmissions.Add(transmission);
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Transmissions.Count.Should().Be(1));

    [Fact]
    public Task Can_Update_Related_Transmission_With_Null_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
            {
                var transmission = UpdateDialogDialogTransmissionDto();
                var relatedTransmission = UpdateDialogDialogTransmissionDto();

                transmission.RelatedTransmissionId = relatedTransmission.Id;
                transmission.Id = null!;

                x.Dto.Transmissions = [transmission, relatedTransmission];
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Transmissions.Count.Should().Be(2));

    [Fact]
    public Task Can_Add_Transmission_Without_Summary_On_Update() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
                x.AddTransmission(x =>
                    x.Content!.Summary = null))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.Transmissions
                    .First().Content.Summary.Should().BeNull());

    [Fact]
    public async Task Cannot_Include_Old_Transmissions_In_UpdateCommand()
    {
        var existingTransmissionId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(count: 1).First();
                transmission.Id = existingTransmissionId;
                x.Dto.Transmissions.Add(transmission);
            })
            .AssertSuccessAndUpdateDialog(x =>
            {
                var transmission = UpdateDialogDialogTransmissionDto();
                transmission.Id = existingTransmissionId;
                x.Dto.Transmissions.Add(transmission);
            })
            .ExecuteAndAssert<DomainError>(error =>
                error.ShouldHaveErrorWithText(existingTransmissionId.ToString()));
    }

    [Fact]
    public Task Cannot_Add_Transmissions_Without_Content_In_IsApiOnlyFalse_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = false)
            .AssertSuccessAndUpdateDialog(x =>
            {
                var newTransmission = UpdateDialogDialogTransmissionDto();
                newTransmission.Content = null!;
                x.Dto.Transmissions.Add(newTransmission);
            })
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText(nameof(DialogTransmission.Content)));

    [Fact]
    public Task Can_Add_Transmissions_Without_Content_In_IsApiOnlyFTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x =>
            {
                var newTransmission = UpdateDialogDialogTransmissionDto();
                newTransmission.Content = null!;
                x.Dto.Transmissions.Add(newTransmission);
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog => dialog
                .Transmissions
                .Single()
                .Content
                .Should()
                .BeNull());

    [Fact]
    public Task Should_Validate_Supplied_Transmission_Content_If_IsApiOnlyTrue_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IsApiOnly = true)
            .AssertSuccessAndUpdateDialog(x =>
            {
                var newTransmission = UpdateDialogDialogTransmissionDto();
                newTransmission.Content!.Title = null!;
                x.Dto.Transmissions.Add(newTransmission);
            })
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText(nameof(ContentDto.Title)));

    [Fact]
    public Task Cannot_Update_Transmission_NavigationalAction_With_Long_Title() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
                x.AddTransmission(transmission =>
                    transmission.AddNavigationalAction(action =>
                        action.Title =
                        [
                            new LocalizationDto
                            {
                                LanguageCode = "nb",
                                Value = new string('a', 256)
                            }
                        ])))
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText("256 characters"));

    [Fact]
    public Task Cannot_Update_Transmission_NavigationalAction_With_Http_Url() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
                x.AddTransmission(transmission =>
                    transmission.AddNavigationalAction(action =>
                        action.Url = new Uri("http://example.com/action"))))
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText("https"));

    [Fact]
    public Task Cannot_Update_Transmission_NavigationalAction_With_ExpiresAt_In_Past() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .AssertSuccessAndUpdateDialog(x =>
                x.AddTransmission(transmission =>
                    transmission.AddNavigationalAction(action =>
                        action.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1))))
            .ExecuteAndAssert<ValidationError>(error =>
                error.ShouldHaveErrorWithText("future"));

    [Fact]
    public Task Can_Update_Transmission_Without_Bumping_ContentUpdatedAt() =>
        FlowBuilder.For(Application)
            .ConfigureServices(ConfigureUserWithScope(AuthorizationScope.ServiceProviderChangeTransmissions))
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>((dialog, ctx) =>
            {
                var transmission = dialog.Transmissions.Single();
                ctx.Bag[TransmissionIdKey] = transmission.Id;
                ctx.Bag[ContentUpdatedAtKey] = dialog.ContentUpdatedAt;
            })
            .UpdateTransmission(ctx => (Guid)ctx.Bag[TransmissionIdKey]!, command =>
            {
                command.IsSilentUpdate = true;
                var localization = command.Dto.Content!.Title.Value.First();
                localization.Value = $"{localization.Value} updated";
            })
            .AssertResult<UpdateTransmissionSuccess>()
            .SendCommand(ctx => new GetDialogQuerySO { DialogId = ctx.GetDialogId() })
            .ExecuteAndAssert<DialogDto>((dialog, ctx) =>
            {
                var transmissionId = (Guid)ctx.Bag[TransmissionIdKey]!;
                var contentUpdatedAt = (DateTimeOffset)ctx.Bag[ContentUpdatedAtKey]!;
                var transmission = dialog.Transmissions.Single(x => x.Id == transmissionId);

                transmission.Content.Title.Value.First().Value.Should().Contain("updated");
                dialog.ContentUpdatedAt.Should().Be(contentUpdatedAt);
            });

    [Fact]
    public Task Update_Transmission_Without_Scope_Is_Forbidden() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>((dialog, ctx) =>
            {
                var transmission = dialog.Transmissions.Single();
                ctx.Bag[TransmissionIdKey] = transmission.Id;
            })
            .UpdateTransmission(ctx => (Guid)ctx.Bag[TransmissionIdKey]!, command =>
            {
                command.IsSilentUpdate = true;
            })
            .ExecuteAndAssert<Forbidden>();

    private static Action<IServiceCollection> ConfigureUserWithScope(string scope) => services =>
    {
        var claims = IntegrationTestUser.GetDefaultClaims();
        claims.Add(new Claim("scope", scope));
        var user = new IntegrationTestUser(claims, addDefaultClaims: false);

        services.RemoveAll<IUser>();
        services.AddSingleton<IUser>(user);
    };

    private static TransmissionDto UpdateDialogDialogTransmissionDto() => new()
    {
        Id = IdentifiableExtensions.CreateVersion7(),
        Type = DialogTransmissionType.Values.Information,
        Sender = new() { ActorType = ActorType.Values.ServiceOwner },
        Content = new()
        {
            Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(1) },
            Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(1) }
        }
    };
}
