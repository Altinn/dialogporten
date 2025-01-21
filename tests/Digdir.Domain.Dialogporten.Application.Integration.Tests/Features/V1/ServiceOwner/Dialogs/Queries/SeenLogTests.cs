using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSeenLogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSeenLogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using GetDialogQueryEndUser = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.GetDialogQuery;
using GetDialogQueryServiceOwner = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.GetDialogQuery;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SeenLogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Get_Dialog_SeenLog_Should_Return_User_Ids_Unhashed()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createCommandResponse = await Application.Send(createDialogCommand);

        // Call EndUser API to trigger SeenLog
        await Application.Send(new GetDialogQueryEndUser { DialogId = createCommandResponse.AsT0.DialogId });

        // Act
        var response = await Application.Send(new GetDialogQueryServiceOwner
        {
            DialogId = createCommandResponse.AsT0.DialogId
        });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();

        result.SeenSinceLastUpdate
            .Single()
            .SeenBy.ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator);
    }

    [Fact]
    public async Task Search_Dialog_SeenLog_Should_Return_User_Ids_Unhashed()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createCommandResponse = await Application.Send(createDialogCommand);

        // Trigger SeenLog
        await Application.Send(new GetDialogQueryEndUser { DialogId = createCommandResponse.AsT0.DialogId });

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
            .SeenSinceLastUpdate
            .Single()
            .SeenBy.ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator);
    }

    [Fact]
    public async Task Get_SeenLog_Should_Return_User_Ids_Unhashed()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createCommandResponse = await Application.Send(createDialogCommand);

        var triggerSeenLogResponse = await Application.Send(new GetDialogQueryEndUser
        {
            DialogId = createCommandResponse.AsT0.DialogId
        });

        var seenLogId = triggerSeenLogResponse.AsT0.SeenSinceLastUpdate.Single().Id;

        // Act
        var response = await Application.Send(new GetSeenLogQuery
        {
            DialogId = createCommandResponse.AsT0.DialogId,
            SeenLogId = seenLogId
        });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();

        result.SeenBy.ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator);
    }

    [Fact]
    public async Task Search_SeenLog_Should_Return_User_Ids_Unhashed()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createCommandResponse = await Application.Send(createDialogCommand);

        // Trigger SeenLog
        await Application.Send(new GetDialogQueryEndUser { DialogId = createCommandResponse.AsT0.DialogId });

        // Act
        var response = await Application.Send(new SearchSeenLogQuery
        {
            DialogId = createCommandResponse.AsT0.DialogId
        });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();

        result.Single()
            .SeenBy.ActorId
            .Should()
            .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator);
    }
}
