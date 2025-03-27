using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using ActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ActivityDto;
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
