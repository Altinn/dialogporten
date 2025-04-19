using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using AttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.AttachmentDto;
using AttachmentUrlDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.AttachmentUrlDto;
using TransmissionAttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionAttachmentDto;
using TransmissionAttachmentUrlDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionAttachmentUrlDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UniqueConstraintTests : ApplicationCollectionFixture
{
    public UniqueConstraintTests(DialogApplication application) : base(application) { }

    // Create
    [Fact]
    public async Task Cannot_Create_Dialog_With_Existing_Id()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        await Application.Send(createDialogCommand);

        // Act
        var duplicateCreateResponse = await Application.Send(createDialogCommand);

        // Assert
        duplicateCreateResponse.TryPickT1(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(dialogId.ToString()));
    }

    [Fact]
    public async Task Cannot_Create_Dialog_Attachment_With_Existing_Id()
    {
        // Arrange
        var attachment = new AttachmentDto()
        {
            Id = IdentifiableExtensions.CreateVersion7(),
            DisplayName = DialogGenerator.GenerateFakeLocalizations(1),
            Urls = [new AttachmentUrlDto
            {
                ConsumerType = AttachmentUrlConsumerType.Values.Api,
                Url = new Uri("https://example.com")
            }]
        };

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Attachments.Add(attachment);

        await Application.Send(createDialogCommand);

        createDialogCommand.Dto.Id = IdentifiableExtensions.CreateVersion7();

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

    [Fact]
    public async Task Cannot_Create_Dialog_With_Existing_IdempotentKey()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var idempotentKey = IdentifiableExtensions.CreateVersion7().ToString();
        createDialogCommand.Dto.IdempotentKey = idempotentKey;
        await Application.Send(createDialogCommand);

        // Act
        var duplicateCreateResponse = await Application.Send(createDialogCommand);

        // Assert
        duplicateCreateResponse.TryPickT4(out var conflict, out _).Should().BeTrue();
        conflict.Should().NotBeNull();

        conflict.ErrorMessage.Should().Contain(idempotentKey);
    }

    [Fact]
    public async Task Cannot_Create_Dialog_Activity_With_Existing_Id()
    {
        // Arrange
        var dialogActivity = DialogGenerator.GenerateFakeDialogActivity();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Activities.Add(dialogActivity);

        await Application.Send(createDialogCommand);

        createDialogCommand.Dto.Id = IdentifiableExtensions.CreateVersion7();

        // Act
        var duplicateCreateResponse = await Application.Send(createDialogCommand);

        // Assert
        duplicateCreateResponse.TryPickT1(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(dialogActivity.Id.ToString()!));
    }

    [Fact]
    public async Task Cannot_Create_Gui_Action_With_Existing_Id()
    {
        // Arrange
        var guiAction = DialogGenerator.GenerateFakeDialogGuiActions()[0];
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.GuiActions.Add(guiAction);

        await Application.Send(createDialogCommand);

        createDialogCommand.Dto.Id = IdentifiableExtensions.CreateVersion7();

        // Act
        var duplicateCreateResponse = await Application.Send(createDialogCommand);

        // Assert
        duplicateCreateResponse.TryPickT1(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(guiAction.Id.ToString()!));
    }

    [Fact]
    public async Task Cannot_Create_Api_Action_With_Existing_Id()
    {
        // Arrange
        var apiAction = DialogGenerator.GenerateFakeDialogApiActions()[0];
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.ApiActions.Add(apiAction);

        await Application.Send(createDialogCommand);

        createDialogCommand.Dto.Id = IdentifiableExtensions.CreateVersion7();

        // Act
        var duplicateCreateResponse = await Application.Send(createDialogCommand);

        // Assert
        duplicateCreateResponse.TryPickT1(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(apiAction.Id.ToString()!));
    }

    [Fact]
    public async Task Cannot_Create_Dialog_Transmission_With_Existing_Id()
    {
        // Arrange
        var dialogTransmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Transmissions.Add(dialogTransmission);

        await Application.Send(createDialogCommand);

        createDialogCommand.Dto.Id = IdentifiableExtensions.CreateVersion7();

        // Act
        var duplicateCreateResponse = await Application.Send(createDialogCommand);

        // Assert
        duplicateCreateResponse.TryPickT1(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(dialogTransmission.Id.ToString()!));
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
            Urls = [new TransmissionAttachmentUrlDto
            {
                ConsumerType = AttachmentUrlConsumerType.Values.Api,
                Url = new Uri("https://example.com")
            }]
        };

        dialogTransmission.Attachments.Add(attachment);

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Transmissions.Add(dialogTransmission);

        await Application.Send(createDialogCommand);

        createDialogCommand.Dto.Id = IdentifiableExtensions.CreateVersion7();

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

    // Update
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
}
