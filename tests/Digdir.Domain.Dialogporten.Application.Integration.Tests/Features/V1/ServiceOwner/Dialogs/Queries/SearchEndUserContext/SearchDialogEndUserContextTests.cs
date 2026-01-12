using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogEndUserContextTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Search_Returns_EndUserContext_Labels_With_Any_Label_Match()
    {
        var dialogId = Guid.Empty;

        return FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Party = IntegrationTestUser.DefaultParty)
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Archive])
            .SearchServiceOwnerDialogEndUserContexts((query, ctx) =>
            {
                dialogId = ctx.GetDialogId();
                query.Party = [ctx.GetParty()];
                query.Label = [SystemLabel.Values.Archive, SystemLabel.Values.Bin];
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>(result =>
                result.Items.Should().ContainSingle(item =>
                    item.DialogId == dialogId &&
                    item.EndUserContextRevision != Guid.Empty &&
                    item.SystemLabels.Contains(SystemLabel.Values.Archive)));
    }

    [Fact]
    public Task Search_Without_Party_Returns_ValidationError() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogEndUserContexts(_ => { })
            .ExecuteAndAssert<ValidationError>();

    [Fact]
    public Task Search_With_EndUserId_And_No_Authorizations_Returns_Empty()
    {
        return FlowBuilder.For(Application, services =>
            {
                services.ConfigureAltinnAuthorization(x =>
                    x.ConfigureGetAuthorizedResourcesForSearch(new DialogSearchAuthorizationResult()));
            })
            .CreateSimpleDialog(x => x.Dto.Party = IntegrationTestUser.DefaultParty)
            .SearchServiceOwnerDialogEndUserContexts((query, ctx) =>
            {
                query.Party = [ctx.GetParty()];
                query.EndUserId = IntegrationTestUser.DefaultParty;
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>(result =>
                result.Items.Should().BeEmpty());
    }

    [Fact]
    public Task Search_Returns_All_System_Labels()
    {
        var dialogId = Guid.Empty;

        return FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Party = IntegrationTestUser.DefaultParty)
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Archive, SystemLabel.Values.MarkedAsUnopened])
            .SearchServiceOwnerDialogEndUserContexts((query, ctx) =>
            {
                dialogId = ctx.GetDialogId();
                query.Party = [ctx.GetParty()];
            })
            .ExecuteAndAssert<PaginatedList<DialogEndUserContextItemDto>>(result =>
            {
                var labels = result.Items.Single(item => item.DialogId == dialogId).SystemLabels;
                labels.Should().Contain(SystemLabel.Values.Archive);
                labels.Should().Contain(SystemLabel.Values.MarkedAsUnopened);
                labels.Distinct().Should().HaveCount(labels.Count);
            });
    }
}
