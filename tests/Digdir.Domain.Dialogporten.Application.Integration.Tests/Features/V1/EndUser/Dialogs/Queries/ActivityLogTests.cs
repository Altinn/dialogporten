using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogActivities.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ActivityLogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Get_Dialog_ActivityLog_Should_Not_Return_User_Ids_Unhashed()
    {
        var (_, createCommandResponse) = await GenerateDialogWithActivity();

        // Act
        var response = await Application.Send(new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();

        result.Activities
            .Single()
            .PerformedBy.ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator);

    }

    [Fact]
    public async Task Search_Dialog_LatestActivity_Should_Not_Return_User_Ids_Unhashed()
    {
        // Arrange
        var (createDialogCommand, _) = await GenerateDialogWithActivity();

        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            ServiceResource = [createDialogCommand.Dto.ServiceResource]
        });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();

        result.Items
            .Single()
            .LatestActivity!
            .PerformedBy.ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator);
    }

    [Fact]
    public async Task Get_ActivityLog_Should_Not_Return_User_Ids_Unhashed()
    {
        // Arrange
        var (_, createCommandResponse) = await GenerateDialogWithActivity();

        var getDialogResult = await Application.Send(new GetDialogQuery
        {
            DialogId = createCommandResponse.AsT0.DialogId
        });

        var activityId = getDialogResult.AsT0.Activities.First().Id;

        // Act
        var response = await Application.Send(new GetActivityQuery
        {
            DialogId = createCommandResponse.AsT0.DialogId,
            ActivityId = activityId
        });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();

        result.PerformedBy.ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator);
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
