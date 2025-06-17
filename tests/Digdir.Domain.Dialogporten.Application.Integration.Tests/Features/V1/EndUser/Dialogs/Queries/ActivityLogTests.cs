using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.DialogActivities.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using ActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.DialogActivities.Queries.Get.ActivityDto;
using ActivityDtoSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ActivityDto;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ActivityLogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Get_Dialog_ActivityLog_Should_Not_Return_User_Ids_Unhashed()
    {
        var activity = CreateActivity();
        var unhashedActorId = activity.PerformedBy.ActorId;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Activities.Add(activity))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                var actorId = result.Activities.Single().PerformedBy.ActorId;
                actorId.Should().StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator);
                actorId.Should().NotContain(unhashedActorId);
            });
    }

    [Fact]
    public async Task Search_Dialog_LatestActivity_Should_Not_Return_User_Ids_Unhashed()
    {
        var activity = CreateActivity();
        var unhashedActorId = activity.PerformedBy.ActorId;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.Dto.Party = unhashedActorId!;
                x.Dto.Activities.Add(activity);
            })
            .SearchEndUserDialogs(x => x.Party = [unhashedActorId!])
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(result =>
            {
                var actorId = result.Items.Single().LatestActivity!.PerformedBy.ActorId;
                actorId.Should().StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator);
                actorId.Should().NotContain(unhashedActorId);
            });
    }

    [Fact]
    public async Task Get_Single_Activity_Should_Not_Return_User_Ids_Unhashed()
    {
        var activity = CreateActivity();
        var activityId = activity.Id!.Value;
        var unhashedActorId = activity.PerformedBy.ActorId;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Activities.Add(activity))
            .SendCommand((_, ctx) => new GetActivityQuery
            {
                DialogId = ctx.GetDialogId(),
                ActivityId = activityId
            })
            .ExecuteAndAssert<ActivityDto>(result =>
            {
                var actorId = result.PerformedBy.ActorId;
                actorId.Should().StartWith(NorwegianPersonIdentifier.HashPrefixWithSeparator);
                actorId.Should().NotContain(unhashedActorId);
            });
    }

    private static ActivityDtoSO CreateActivity()
    {
        var activity = DialogGenerator.GenerateFakeDialogActivity(type: DialogActivityType.Values.Information);
        activity.PerformedBy.ActorId = DialogGenerator.GenerateRandomParty(forcePerson: true);
        activity.PerformedBy.ActorName = null;
        return activity;
    }
}
