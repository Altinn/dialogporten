using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetActivity;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogDto;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.DialogDto;
using ActivityDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.GetActivity.ActivityDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ActivityLogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Dialog_ActivityLog_Should_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateDialog(_ => DialogWithActivity())
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Activities.Single()
                    .PerformedBy
                    .ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator));

    [Fact]
    public Task Search_Dialog_LatestActivity_Should_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateDialog(_ => DialogWithActivity())
            .SendCommand((_, ctx) => new SearchDialogQuery
            {
                ServiceResource = [ctx.GetServiceResource()]
            })
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(x =>
                x.Items.Single()
                    .LatestActivity!
                    .PerformedBy
                    .ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator));

    [Fact]
    public Task Get_ActivityLog_Should_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateDialog(_ => DialogWithActivity())
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>()
            .SendCommand(x => new GetActivityQuery
            {
                DialogId = x.Id,
                ActivityId = x.Activities.First().Id
            })
            .ExecuteAndAssert<ActivityDto>(x =>
                x.PerformedBy
                    .ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator));

    private static CreateDialogCommand DialogWithActivity()
    {
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var activity = DialogGenerator.GenerateFakeDialogActivity(type: DialogActivityType.Values.Information);
        activity.PerformedBy.ActorId = DialogGenerator.GenerateRandomParty(forcePerson: true);
        activity.PerformedBy.ActorName = null;
        createDialogCommand.Dto.Activities.Add(activity);
        return createDialogCommand;
    }
}
