using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Parties;
using FluentAssertions;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogDto;
using SearchDialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.DialogDto;
using SeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSeenLogs.Queries.Get.SeenLogDto;
using SearchSeenLogDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSeenLogs.Queries.Search.SeenLogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SeenLogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Dialog_SeenLog_Should_Return_User_Ids_Unhashed()
        => FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(dialog =>
                dialog.SeenSinceLastUpdate
                    .Single()
                    .SeenBy.ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator));

    [Fact]
    public Task Search_Dialog_SeenLog_Should_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .SearchServiceOwnerDialogs((x, ctx) => x.ServiceResource = [ctx.GetServiceResource()])
            .ExecuteAndAssert<PaginatedList<SearchDialogDto>>(result =>
                result.Items
                    .Single()
                    .SeenSinceLastUpdate
                    .Single()
                    .SeenBy.ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator));

    [Fact]
    public Task Get_SeenLog_Should_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .GetServiceOwnerSeenLogEntry((query, dialog) =>
            {
                query.DialogId = dialog.Id;
                query.SeenLogId = dialog.SeenSinceLastUpdate.Single().Id;
            })
            .ExecuteAndAssert<SeenLogDto>(result =>
                result.SeenBy
                    .ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator));

    [Fact]
    public Task Search_SeenLog_Should_Return_User_Ids_Unhashed() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .GetServiceOwnerSeenLog()
            .ExecuteAndAssert<List<SearchSeenLogDto>>(result =>
                result.Single()
                    .SeenBy
                    .ActorId
                    .Should()
                    .StartWith(NorwegianPersonIdentifier.PrefixWithSeparator));
}
