using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogTests : ApplicationCollectionFixture
{
    public SearchDialogTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Freetext_Search_With_Valid_SearchTerm_Returns_Success() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogs(x =>
            {
                x.Search = "foobar";
                x.Party = [DialogGenerator.GenerateRandomParty()];
                x.EndUserId = DialogGenerator.GenerateRandomParty(forcePerson: true);
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().BeEmpty());

    [Fact]
    public Task Freetext_Search_Without_EndUserId_Results_In_ValidationError() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogs(x => x.Search = "foobar")
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(nameof(SearchDialogQuery.EndUserId)));

    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public Task Search_Should_Populate_Obsolete_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SearchServiceOwnerDialogs((x, ctx) => x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().ContainSingle(x =>
                    x.SystemLabel == SystemLabel.Values.Default));

    [Fact]
    public Task Search_Should_Return_Number_Of_Transmissions_From_Party_And_ServiceOwner() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                .AddTransmission(x => x.Type = DialogTransmissionType.Values.Alert)
                .AddTransmission(x => x.Type = DialogTransmissionType.Values.Submission))
            .SearchServiceOwnerDialogs((x, ctx) => x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
            {
                var dialog = x.Items.Single();
                dialog.FromPartyTransmissionsCount.Should().Be(1);
                dialog.FromServiceOwnerTransmissionsCount.Should().Be(1);
            });

    [Fact]
    public Task Filter_On_IsContentSeen_True_Should_Return_Expected_Dialogs_When_Dialog_Created_And_Opened_And_Marked_As_Unopened_And_Opened_Again() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .ConsumeEvents()
            .SearchServiceOwnerDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.IsContentSeen = true;
            })
            .AssertResult<PaginatedList<DialogDto>>(x => x.Items.Should().HaveCount(0))
            .GetEndUserDialog()
            .ConsumeEvents()
            .SearchServiceOwnerDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.IsContentSeen = true;
            })
            .AssertResult<PaginatedList<DialogDto>>((x, ctx) =>
            {
                var dialog = x.Items.Single();
                dialog.Id.Should().Be(ctx.GetDialogId());
                dialog.IsContentSeen.Should().BeTrue();
            })
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.MarkedAsUnopened])
            .SearchServiceOwnerDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.IsContentSeen = true;
            })
            .AssertResult<PaginatedList<DialogDto>>(x => x.Items.Should().HaveCount(0))
            .GetEndUserDialog()
            .ConsumeEvents()
            .SearchServiceOwnerDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.IsContentSeen = true;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>((x, ctx) =>
            {
                var dialog = x.Items.Single();
                dialog.Id.Should().Be(ctx.GetDialogId());
                dialog.IsContentSeen.Should().BeTrue();
            });

    [Fact]
    public Task Filter_On_IsContentSeen_False_Should_Return_Expected_Dialogs_When_Dialog_Created_And_Opened_And_Marked_As_Unopened_And_Opened_Again() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .ConsumeEvents()
            .SearchServiceOwnerDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.IsContentSeen = false;
            })
            .AssertResult<PaginatedList<DialogDto>>(x =>
            {
                var dialogs = x.Items;
                dialogs.Should().HaveCount(2);
                dialogs.Should().AllSatisfy(d => d.IsContentSeen.Should().BeFalse());
            })
            .GetEndUserDialog()
            .ConsumeEvents()
            .SearchServiceOwnerDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.IsContentSeen = false;
            })
            .AssertResult<PaginatedList<DialogDto>>((x, ctx) =>
            {
                var dialog = x.Items.Single();
                dialog.Id.Should().NotBe(ctx.GetDialogId());
                dialog.IsContentSeen.Should().BeFalse();
            })
            .SetSystemLabelsEndUser(x => x.AddLabels = [SystemLabel.Values.MarkedAsUnopened])
            .SearchServiceOwnerDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.IsContentSeen = false;
            })
            .AssertResult<PaginatedList<DialogDto>>(x =>
            {
                var dialogs = x.Items;
                dialogs.Should().HaveCount(2);
                dialogs.Should().AllSatisfy(d => d.IsContentSeen.Should().BeFalse());
            })
            .GetEndUserDialog()
            .ConsumeEvents()
            .SearchServiceOwnerDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.IsContentSeen = false;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>((x, ctx) =>
            {
                var dialog = x.Items.Single();
                dialog.Id.Should().NotBe(ctx.GetDialogId());
                dialog.IsContentSeen.Should().BeFalse();
            });
}
