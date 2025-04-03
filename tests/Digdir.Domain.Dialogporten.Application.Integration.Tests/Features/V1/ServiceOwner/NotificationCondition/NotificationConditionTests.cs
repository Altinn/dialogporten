using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogActivities.Queries.NotificationCondition;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.NotificationCondition;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class NotificationConditionTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private static readonly bool[] ExpectedSendNotificationsValues = [true, false];

    public static IEnumerable<object[]> NotificationConditionTestData() =>
        from bool expectedSendNotificationValue in ExpectedSendNotificationsValues
        from DialogActivityType.Values activityType in Enum.GetValues(typeof(DialogActivityType.Values))
        from NotificationConditionType conditionType in Enum.GetValues(typeof(NotificationConditionType))
        select new object[] { activityType, conditionType, expectedSendNotificationValue };

    [Theory, MemberData(nameof(NotificationConditionTestData))]
    public async Task SendNotification_Should_Be_True_When_Conditions_Are_Met(
        DialogActivityType.Values activityType,
        NotificationConditionType conditionType,
        bool expectedSendNotificationValue)
    {
        // Arrange
        var userWithLegacyScope = new IntegrationTestUser([new("scope", AuthorizationScope.CorrespondenceScope)]);
        Application.ConfigureServices(services =>
        {
            services.RemoveAll<IUser>();
            services.AddSingleton<IUser>(userWithLegacyScope);
        });

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        switch (conditionType)
        {
            case NotificationConditionType.Exists when expectedSendNotificationValue:
            case NotificationConditionType.NotExists when !expectedSendNotificationValue:
                AddActivityRequirements(createDialogCommand, activityType);
                break;
        }

        var response = await Application.Send(createDialogCommand);
        response.TryPickT0(out var dialogId, out _);

        var notificationConditionQuery = CreateNotificationConditionQuery(dialogId.DialogId, activityType, conditionType);

        if (activityType is DialogActivityType.Values.TransmissionOpened)
        {
            var transmissionId = createDialogCommand.Dto.Transmissions.FirstOrDefault()?.Id ?? Guid.NewGuid();
            notificationConditionQuery.TransmissionId = transmissionId;
        }

        // Act
        var queryResult = await Application.Send(notificationConditionQuery);

        // Assert
        queryResult.TryPickT0(out var notificationConditionResult, out _);
        queryResult.IsT0.Should().BeTrue();
        notificationConditionResult.SendNotification.Should().Be(expectedSendNotificationValue);
    }

    private static void AddActivityRequirements(
        CreateDialogCommand createDialogCommand,
        DialogActivityType.Values activityType)
    {
        var activity = DialogGenerator.GenerateFakeDialogActivity(type: activityType);
        createDialogCommand.Dto.Activities.Add(activity);

        if (activityType is not DialogActivityType.Values.TransmissionOpened) return;

        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(type: DialogTransmissionType.Values.Information)[0];
        createDialogCommand.Dto.Transmissions.Add(transmission);
        createDialogCommand.Dto.Activities[0].TransmissionId = createDialogCommand.Dto.Transmissions[0].Id;
    }

    [Fact]
    public async Task NotFound_Should_Be_Returned_When_Dialog_Does_Not_Exist()
    {
        // Arrange
        var notificationConditionQuery = CreateNotificationConditionQuery(Guid.NewGuid());

        // Act
        var queryResult = await Application.Send(notificationConditionQuery);

        // Assert
        queryResult.TryPickT2(out var notFound, out _);
        queryResult.IsT2.Should().BeTrue();
        notFound.Should().NotBeNull();
    }

    [Fact]
    public async Task Gone_Should_Be_Returned_When_Dialog_Is_Deleted()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();

        var response = await Application.Send(createDialogCommand);
        response.TryPickT0(out var success, out _);

        await Application.Send(new DeleteDialogCommand { Id = success.DialogId });

        var notificationConditionQuery = CreateNotificationConditionQuery(success.DialogId);

        // Act
        var queryResult = await Application.Send(notificationConditionQuery);

        // Assert
        queryResult.TryPickT3(out var deleted, out _);
        queryResult.IsT3.Should().BeTrue();
        deleted.Should().NotBeNull();
        deleted.Message.Should().Contain(success.DialogId.ToString());
    }

    private static NotificationConditionQuery CreateNotificationConditionQuery(Guid dialogId,
        DialogActivityType.Values activityType = DialogActivityType.Values.Information,
        NotificationConditionType conditionType = NotificationConditionType.Exists)
        => new()
        {
            DialogId = dialogId,
            ActivityType = activityType,
            ConditionType = conditionType,
        };
}
