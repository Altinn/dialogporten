using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.NotificationCondition;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
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
        Guid? transmissionId = null;
        await FlowBuilder.For(Application, x =>
            {
                x.RemoveAll<IUser>();
                x.AddSingleton<IUser>(new IntegrationTestUser([new("scope", AuthorizationScope.CorrespondenceScope)]));
            })
            .CreateSimpleDialog(x =>
            {
                switch (conditionType)
                {
                    case NotificationConditionType.Exists when expectedSendNotificationValue:
                    case NotificationConditionType.NotExists when !expectedSendNotificationValue:
                        AddActivityRequirements(x, activityType);
                        break;
                }

                if (activityType is DialogActivityType.Values.TransmissionOpened)
                {
                    transmissionId = x.Dto.Transmissions.FirstOrDefault()?.Id ?? Guid.NewGuid();
                }
            })
            .AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) => new NotificationConditionQuery
            {
                DialogId = ctx.GetDialogId(),
                ActivityType = activityType,
                ConditionType = conditionType,
                TransmissionId = transmissionId
            })
            .ExecuteAndAssert<NotificationConditionDto>(x =>
                x.SendNotification.Should().Be(expectedSendNotificationValue));
    }

    [Theory, ClassData(typeof(TransmissionNotificationConditionTestData))]
    public Task Bad_Request_On_TransmissionId_When_ActivityType_Is_Not_TransmissionOpened(DialogActivityType.Values activityType) =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new NotificationConditionQuery
            {
                DialogId = ctx.GetDialogId(),
                ActivityType = activityType,
                ConditionType = NotificationConditionType.Exists,
                TransmissionId = Guid.NewGuid()
            })
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(DialogActivityType.Values.TransmissionOpened)));

    public sealed class TransmissionNotificationConditionTestData : TheoryData<DialogActivityType.Values>
    {
        public TransmissionNotificationConditionTestData()
        {
            var invalidActivityTypes = Enum
                .GetValues(typeof(DialogActivityType.Values))
                .Cast<DialogActivityType.Values>()
                .Where(x => x != DialogActivityType.Values.TransmissionOpened);

            foreach (var activityType in invalidActivityTypes)
            {
                Add(activityType);
            }
        }
    }

    [Fact]
    public Task NotFound_Should_Be_Returned_When_Dialog_Does_Not_Exist() =>
        FlowBuilder.For(Application)
            .SendCommand(_ => new NotificationConditionQuery
            {
                DialogId = Guid.NewGuid(),
                ActivityType = DialogActivityType.Values.Information,
                ConditionType = NotificationConditionType.Exists
            })
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();

    [Fact]
    public Task Gone_Should_Be_Returned_When_Dialog_Is_Deleted() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .DeleteDialog()
            .AssertResult<DeleteDialogSuccess>()
            .SendCommand((_, ctx) => new NotificationConditionQuery
            {
                DialogId = ctx.GetDialogId(),
                ActivityType = DialogActivityType.Values.Information,
                ConditionType = NotificationConditionType.Exists
            })
            .ExecuteAndAssert<EntityDeleted<DialogEntity>>();

    private static void AddActivityRequirements(
        CreateDialogCommand createDialogCommand,
        DialogActivityType.Values activityType)
    {
        var activity = DialogGenerator.GenerateFakeDialogActivity(type: activityType);
        createDialogCommand.Dto.Activities.Add(activity);

        if (activityType is not DialogActivityType.Values.TransmissionOpened) return;

        var transmission =
            DialogGenerator.GenerateFakeDialogTransmissions(type: DialogTransmissionType.Values.Information)[0];
        createDialogCommand.Dto.Transmissions.Add(transmission);
        createDialogCommand.Dto.Activities[0].TransmissionId = createDialogCommand.Dto.Transmissions[0].Id;
    }
}
