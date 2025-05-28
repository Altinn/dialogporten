using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using TransmissionAttachmentDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionAttachmentDto;
using TransmissionAttachmentUrlDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.
    TransmissionAttachmentUrlDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UniqueConstraintTests : ApplicationCollectionFixture
{
    public UniqueConstraintTests(DialogApplication application) : base(application) { }

    # region Create

    private sealed class ExistingDbIdTestData : TheoryData<string, Action<CreateDialogCommand>, string>
    {
        public ExistingDbIdTestData()
        {
            var dialogId = NewUuidV7();
            Add("Cannot create dialog with existing id",
                x => x.Dto.Id = dialogId,
                dialogId.ToString());

            var dialogAttachment = DialogGenerator.GenerateFakeDialogAttachment();
            Add("Cannot create dialog attachment with existing attachment id",
                x => x.Dto.Attachments.Add(dialogAttachment),
                dialogAttachment.Id.ToString()!);

            var dialogActivity = DialogGenerator.GenerateFakeDialogActivity();
            Add("Cannot create dialog activity with existing id",
                x => x.Dto.Activities.Add(dialogActivity),
                dialogActivity.Id.ToString()!);

            var guiAction = DialogGenerator.GenerateFakeDialogGuiActions()[0];
            Add("Cannot create dialog gui action with existing id",
                x => x.Dto.GuiActions.Add(guiAction),
                guiAction.Id.ToString()!);

            var apiAction = DialogGenerator.GenerateFakeDialogApiActions()[0];
            Add("Cannot create dialog api action with existing id",
                x => x.Dto.ApiActions.Add(apiAction),
                apiAction.Id.ToString()!);

            var dialogTransmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
            Add("Cannot create dialog transmission with existing id",
                x => x.Dto.Transmissions.Add(dialogTransmission),
                dialogTransmission.Id.ToString()!);

            var transmissionAttachment = new TransmissionAttachmentDto
            {
                Id = NewUuidV7(),
                DisplayName = DialogGenerator.GenerateFakeLocalizations(1),
                Urls =
                [
                    new()
                    {
                        ConsumerType = AttachmentUrlConsumerType.Values.Api,
                        Url = new Uri("https://example.com")
                    }
                ]
            };
            Add("Cannot create transmission attachment with existing id",
                x =>
                {
                    var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                    transmission.Attachments.Add(transmissionAttachment);
                    x.Dto.Transmissions.Add(transmission);
                },
                transmissionAttachment.Id.ToString()!);
        }
    }

    [Theory, ClassData(typeof(ExistingDbIdTestData))]
    public Task Existing_Database_Ids_Tests(string _,
        Action<CreateDialogCommand> createDialogCommand, string conflictingId) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(createDialogCommand)
            .CreateSimpleDialog(createDialogCommand)
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText(conflictingId));

    [Fact]
    public async Task Cannot_Use_Existing_IdempotentKey_When_Creating_Dialog()
    {
        var idempotentKey = NewUuidV7().ToString();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.IdempotentKey = idempotentKey)
            .CreateSimpleDialog(x => x.Dto.IdempotentKey = idempotentKey)
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText(idempotentKey));
    }

    [Fact]
    public async Task Cannot_Create_Transmission_Attachment_With_Existing_Id()
    {
        // Arrange
        var dialogTransmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
        var attachment = new TransmissionAttachmentDto
        {
            Id = IdentifiableExtensions.CreateVersion7(),
            DisplayName = DialogGenerator.GenerateFakeLocalizations(1),
            Urls =
            [
                new TransmissionAttachmentUrlDto
                {
                    ConsumerType = AttachmentUrlConsumerType.Values.Api,
                    Url = new Uri("https://example.com")
                }
            ]
        };

        dialogTransmission.Attachments.Add(attachment);

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Transmissions.Add(dialogTransmission);

        await Application.Send(createDialogCommand);

        createDialogCommand.Dto.Id = IdentifiableExtensions.CreateVersion7();
        dialogTransmission.Id = IdentifiableExtensions.CreateVersion7();

        // Act
        var duplicateCreateResponse = await Application.Send(createDialogCommand);

        // Assert
        duplicateCreateResponse.TryPickT1(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(attachment.Id.ToString()!));
    }

    #endregion

    # region Update

    [Fact]
    public async Task Cannot_Append_Transmission_With_Existing_Id()
    {
        // Arrange
        var dialogTransmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        createDialogCommand.Dto.Transmissions.Add(dialogTransmission);

        await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = dialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.Transmissions.Clear();

        // Append transmission
        updateDialogDto.Transmissions.Add(new()
        {
            Id = dialogTransmission.Id,
            Type = DialogTransmissionType.Values.Rejection,
            Sender = new() { ActorType = ActorType.Values.ServiceOwner },
            Content = new()
            {
                Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(1) },
                Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(1) }
            }
        });

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = dialogId,
            Dto = updateDialogDto,
            IsSilentUpdate = true
        });

        updateResponse.TryPickT5(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(dialogTransmission.Id.ToString()!));
    }

    [Fact]
    public async Task Cannot_Append_Activity_With_Existing_Id()
    {
        // Arrange
        var activity = DialogGenerator.GenerateFakeDialogActivities(1)[0];
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        createDialogCommand.Dto.Activities.Add(activity);

        await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = dialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.Activities.Clear();

        // Append transmission
        updateDialogDto.Activities.Add(new()
        {
            Id = activity.Id,
            PerformedBy = new() { ActorType = ActorType.Values.ServiceOwner },
            Type = DialogActivityType.Values.DialogClosed
        });

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = dialogId,
            Dto = updateDialogDto,
            IsSilentUpdate = true
        });

        updateResponse.TryPickT5(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(activity.Id.ToString()!));
    }

    #endregion
}
