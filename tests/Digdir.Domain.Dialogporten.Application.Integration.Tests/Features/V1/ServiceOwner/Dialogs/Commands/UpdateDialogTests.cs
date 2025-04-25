using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using ActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ActivityDto;
using ApiActionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ApiActionDto;
using AttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.AttachmentDto;
using GuiActionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.GuiActionDto;
using TransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.TransmissionDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task UpdateDialogCommand_Should_Set_New_Revision_If_IsSilentUpdate_Is_Set()
    {
        // Arrange
        var createCommandResponse = await Application.Send(DialogGenerator.GenerateSimpleFakeCreateDialogCommand());

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);
        var oldRevision = getDialogDto.AsT0.Revision;

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        // Update progress
        updateDialogDto.Progress = (updateDialogDto.Progress % 100) + 1;

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto,
            IsSilentUpdate = true
        });

        // Assert
        updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        success.Revision.Should().NotBeEmpty();
        success.Revision.Should().NotBe(oldRevision);
    }


    [Fact]
    public async Task UpdateDialogCommand_Should_Not_Set_SystemLabel_If_IsSilentUpdate_Is_Set()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.SystemLabel = SystemLabel.Values.Bin;
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.SearchTags.Add(new() { Value = "crouching tiger, hidden update" });

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto,
            IsSilentUpdate = true
        });

        updateResponse.TryPickT0(out _, out _).Should().BeTrue();

        var getDialogDtoAfterUpdate = await Application.Send(getDialogQuery);

        // Assert
        getDialogDtoAfterUpdate.AsT0.SystemLabel.Should().Be(getDialogDto.AsT0.SystemLabel);
    }

    [Fact]
    public async Task UpdateDialogCommand_Should_Not_Set_UpdatedAt_If_IsSilentUpdate_Is_Set()
    {
        // Arrange
        var createCommandResponse = await Application.Send(DialogGenerator.GenerateSimpleFakeCreateDialogCommand());

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.Process = "updated:process";

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto,
            IsSilentUpdate = true
        });

        updateResponse.TryPickT0(out _, out _).Should().BeTrue();

        var getDialogDtoAfterUpdate = await Application.Send(getDialogQuery);

        // Assert
        getDialogDtoAfterUpdate.AsT0.UpdatedAt.Should().Be(getDialogDto.AsT0.UpdatedAt);
    }

    [Fact]
    public async Task UpdateDialogCommand_Should_Return_New_Revision()
    {
        // Arrange
        var createCommandResponse = await Application.Send(DialogGenerator.GenerateSimpleFakeCreateDialogCommand());

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);
        var oldRevision = getDialogDto.AsT0.Revision;

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        // Update progress
        updateDialogDto.Progress = (updateDialogDto.Progress % 100) + 1;

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        success.Revision.Should().NotBeEmpty();
        success.Revision.Should().NotBe(oldRevision);
    }

    [Fact]
    public async Task Cannot_Include_Old_Activities_To_UpdateCommand()
    {
        // Arrange
        var (_, createCommandResponse) = await GenerateDialogWithActivity();
        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        // Ref. old activity
        updateDialogDto.Activities.Add(new ActivityDto
        {
            Id = getDialogDto.AsT0.Activities.First().Id,
            Type = DialogActivityType.Values.DialogCreated,
            PerformedBy = new ActorDto
            {
                ActorType = ActorType.Values.ServiceOwner
            }
        });

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT5(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors.Should().Contain(e => e.ErrorMessage.Contains("already exists"));
    }

    [Fact]
    public async Task Cannot_Include_Old_Transmissions_In_UpdateCommand()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var existingTransmission = DialogGenerator.GenerateFakeDialogTransmissions(count: 1).First();
        createDialogCommand.Dto.Transmissions.Add(existingTransmission);
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        // Ref. old transmission
        updateDialogDto.Transmissions.Add(new TransmissionDto
        {
            Id = existingTransmission.Id,
            Type = DialogTransmissionType.Values.Information,
            Sender = new() { ActorType = ActorType.Values.ServiceOwner },
            Content = new()
            {
                Title = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) },
                Summary = new() { Value = DialogGenerator.GenerateFakeLocalizations(3) }
            }
        });

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT5(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors.Should().Contain(e => e.ErrorMessage.Contains("already exists"));
    }

    [Fact]
    public async Task Cannot_Update_Content_To_Null_If_IsApiOnlyFalse_Dialog()
    {
        // Arrange
        var createCommandResponse = await Application.Send(DialogGenerator.GenerateSimpleFakeCreateDialogCommand());

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.Content = null!;

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT3(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
    }

    [Fact]
    public async Task Can_Update_Content_To_Null_If_IsApiOnlyTrue_Dialog()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.IsApiOnly = true;
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.Content = null!;

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Validate_Supplied_Content_If_IsApiOnlyTrue_Dialog()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.IsApiOnly = true;
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);
        updateDialogDto.Content!.Title = null!; // Content is supplied, but title is not (only summary)

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });

        // Assert
        updateResponse.TryPickT3(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_Attachement()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        var attachment = new AttachmentDto
        {
            Id = Guid.CreateVersion7(),
            DisplayName = [new() { LanguageCode = "nb", Value = "Test attachment" }],
            Urls = [new() { Url = new Uri("https://example.com"), ConsumerType = AttachmentUrlConsumerType.Values.Gui }]
        };
        updateDialogDto.Attachments.Add(attachment);

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });
        var getDialogDtoAfterUpdate = await Application.Send(getDialogQuery);

        // Assert
        updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        getDialogDtoAfterUpdate.TryPickT0(out var currentDialog, out _).Should().BeTrue();
        currentDialog.Attachments.Should().ContainSingle(x => x.Id == attachment.Id);
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_ApiAction()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        var apiAction = new ApiActionDto
        {
            Id = Guid.CreateVersion7(),
            Action = "Test action",
            Name = "Test action",
            Endpoints = [new() { Url = new("https://example.com"), HttpMethod = HttpVerb.Values.GET }]
        };
        updateDialogDto.ApiActions.Add(apiAction);

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });
        var getDialogDtoAfterUpdate = await Application.Send(getDialogQuery);

        // Assert
        updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        getDialogDtoAfterUpdate.TryPickT0(out var currentDialog, out _).Should().BeTrue();
        currentDialog.ApiActions.Should().Contain(x => x.Id == apiAction.Id);
    }

    [Fact]
    public async Task Should_Allow_User_Defined_Id_For_GuiAction()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createCommandResponse = await Application.Send(createDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        var guiAction = new GuiActionDto
        {
            Id = Guid.CreateVersion7(),
            Action = "Test action",
            Title = [new() { LanguageCode = "nb", Value = "Test action" }],
            Priority = DialogGuiActionPriority.Values.Tertiary,
            Url = new Uri("https://example.com"),
        };
        updateDialogDto.GuiActions.Add(guiAction);

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand
        {
            Id = createCommandResponse.AsT0.DialogId,
            Dto = updateDialogDto
        });
        var getDialogDtoAfterUpdate = await Application.Send(getDialogQuery);

        // Assert
        updateResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        getDialogDtoAfterUpdate.TryPickT0(out var currentDialog, out _).Should().BeTrue();
        currentDialog.GuiActions.Should().Contain(x => x.Id == guiAction.Id);
    }

    private async Task<(CreateDialogCommand, CreateDialogResult)> GenerateDialogWithActivity()
    {
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var activity = DialogGenerator.GenerateFakeDialogActivity(type: DialogActivityType.Values.Information);
        activity.PerformedBy.ActorId = DialogGenerator.GenerateRandomParty(forcePerson: true);
        activity.PerformedBy.ActorName = null;
        createDialogCommand.Dto.Activities.Add(activity);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return (createDialogCommand, createCommandResponse);
    }
}
