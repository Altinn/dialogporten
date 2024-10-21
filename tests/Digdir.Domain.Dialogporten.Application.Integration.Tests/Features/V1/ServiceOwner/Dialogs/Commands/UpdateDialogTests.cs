﻿using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Cannot_Include_Old_Activities_To_UpdateCommand()
    {
        // Arrange
        var (_, createCommandResponse) = await GenerateDialogWithActivity();
        var getDialogQuery = new GetDialogQuery { DialogId = createCommandResponse.AsT0.Value };
        var getDialogDto = await Application.Send(getDialogQuery);

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(getDialogDto.AsT0);

        // Ref. old activity
        updateDialogDto.Activities.Add(new UpdateDialogDialogActivityDto
        {
            Id = getDialogDto.AsT0.Activities.First().Id,
            Type = DialogActivityType.Values.DialogCreated,
            PerformedBy = new UpdateDialogDialogActivityPerformedByActorDto
            {
                ActorType = ActorType.Values.ServiceOwner
            }
        });

        // Act
        var updateResponse = await Application.Send(new UpdateDialogCommand { Id = createCommandResponse.AsT0.Value, Dto = updateDialogDto });

        // Assert
        updateResponse.TryPickT5(out var domainError, out _).Should().BeTrue();
        domainError.Should().NotBeNull();
        domainError.Errors.Should().Contain(e => e.ErrorMessage.Contains("already exists"));
    }

    private async Task<(CreateDialogCommand, CreateDialogResult)> GenerateDialogWithActivity()
    {
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeDialog();
        var activity = DialogGenerator.GenerateFakeDialogActivity(type: DialogActivityType.Values.Information);
        activity.PerformedBy.ActorId = DialogGenerator.GenerateRandomParty(forcePerson: true);
        activity.PerformedBy.ActorName = null;
        createDialogCommand.Activities.Add(activity);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return (createDialogCommand, createCommandResponse);
    }
}
