using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Should_Filter_On_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog((x, ctx) =>
            {
                x.Dto.SystemLabel = SystemLabel.Values.Bin;
                x.Dto.Party = ctx.GetParty();
            })
            .SearchEndUserDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.SystemLabel = [SystemLabel.Values.Bin];
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
            {
                var dialog = x.Items.Single();

                // Obsolete SystemLabel
                dialog.SystemLabel.Should().Be(SystemLabel.Values.Bin);

                dialog.EndUserContext.SystemLabels.Should().HaveCount(1);
                dialog.EndUserContext.SystemLabels.Should()
                    .ContainSingle(s => s == SystemLabel.Values.Bin);
            });

    [Fact]
    public Task Filter_On_Bin_And_Archive_Should_Return_No_Dialogs() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog((x, ctx) =>
            {
                x.Dto.SystemLabel = SystemLabel.Values.Bin;
                x.Dto.Party = ctx.GetParty();
            })
            .CreateSimpleDialog((x, ctx) =>
            {
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
                x.Dto.Party = ctx.GetParty();
            })
            .SearchEndUserDialogs((x, ctx) =>
            {
                x.Party = [ctx.GetParty()];
                x.SystemLabel = [SystemLabel.Values.Bin, SystemLabel.Values.Archive];
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().HaveCount(0));

    [Fact]
    public async Task Search_Should_Populate_EnduserContextRevision()
    {
        string? party = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => party = x.Dto.Party)
            .SearchEndUserDialogs(x => x.Party = [party!])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().ContainSingle(x =>
                    x.EndUserContext.Revision != Guid.Empty));
    }

    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public async Task Search_Should_Populate_Obsolete_SystemLabel()
    {
        string? party = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => party = x.Dto.Party)
            .SearchEndUserDialogs(x => x.Party = [party!])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().ContainSingle(x =>
                    x.SystemLabel == SystemLabel.Values.Default));
    }

    [Fact]
    public Task Search_Should_Return_HasUnopenedContent_False_For_New_Simple_Dialogs() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SearchEndUserDialogs((x, ctx) =>
                x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Single().HasUnopenedContent.Should().BeFalse());

    [Fact]
    public Task Search_Should_Return_HasUnopenedContent_True_For_Dialogs_With_Unopened_Transmission() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                // Unopened content
                x.AddTransmission(x =>
                    x.Type = DialogTransmissionType.Values.Information))
            .SearchEndUserDialogs((x, ctx) =>
                x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Single().HasUnopenedContent.Should().BeTrue());

    [Fact]
    public Task Search_Should_Return_Number_Of_Transmissions_From_Party_And_ServiceOwner() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x
                .AddTransmission(x => x.Type = DialogTransmissionType.Values.Alert)
                .AddTransmission(x => x.Type = DialogTransmissionType.Values.Submission))
            .SearchEndUserDialogs((x, ctx) => x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
            {
                var dialog = x.Items.Single();
                dialog.FromPartyTransmissionsCount.Should().Be(1);
                dialog.FromServiceOwnerTransmissionsCount.Should().Be(1);
            });

    private const string DummyService = "urn:altinn:resource:test-service";

    [Fact]
    public async Task Search_Should_Return_Delegated_Instances()
    {
        var delegatedDialogId = NewUuidV7();
        var delegatedDialogParty = NorwegianPersonIdentifier.PrefixWithSeparator + "13213312833";

        await FlowBuilder.For(Application, x =>
            {
                x.ConfigureAltinnAuthorization(x =>
                {
                    x.ConfigureGetAuthorizedResourcesForSearch(
                        new DialogSearchAuthorizationResult
                        {
                            ResourcesByParties = new Dictionary<string, HashSet<string>>
                            {
                                // Default integration test user party
                                { TestUsers.DefaultParty, [DummyService] }
                            },
                            // Delegated dialog
                            DialogIds = [delegatedDialogId]
                        });
                });
            })
            .CreateSimpleDialog((x, _) =>
            {
                // Delegated dialog
                x.Dto.ServiceResource = DummyService;
                x.Dto.Id = delegatedDialogId;
                x.Dto.Party = delegatedDialogParty;
            })
            .CreateSimpleDialog((x, _) =>
            {
                // Default integration test user dialog
                x.Dto.ServiceResource = DummyService;
                x.Dto.Party = TestUsers.DefaultParty;
            })
            .SearchEndUserDialogs(x => x.ServiceResource = [DummyService])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
            {
                x.Items.Should().HaveCount(2);

                // Delegated dialog
                x.Items.Should().ContainSingle(d =>
                    d.Id == delegatedDialogId &&
                    d.Party == delegatedDialogParty);

                // Default integration test user dialog
                x.Items.Should().ContainSingle(d =>
                    d.Id != delegatedDialogId &&
                    d.Party == TestUsers.DefaultParty);
            });
    }

    [Fact]
    public async Task Search_Should_Not_Truncate_Page_Two()
    {
        var dialogId1 = NewUuidV7();
        var dialogId2 = NewUuidV7();
        var dialogId3 = NewUuidV7();
        var dialogId4 = NewUuidV7();
        var dialogId5 = NewUuidV7();
        var dialogId6 = NewUuidV7();
        var createdAtBase = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => ConfDialog(x, dialogId1, createdAtBase.AddMinutes(1)))
            .CreateSimpleDialog((x, _) => ConfDialog(x, dialogId2, createdAtBase.AddMinutes(2)))
            .CreateSimpleDialog((x, _) => ConfDialog(x, dialogId3, createdAtBase.AddMinutes(3)))
            .CreateSimpleDialog((x, _) => ConfDialog(x, dialogId4, createdAtBase.AddMinutes(4)))
            .CreateSimpleDialog((x, _) => ConfDialog(x, dialogId5, createdAtBase.AddMinutes(5)))
            .CreateSimpleDialog((x, _) => ConfDialog(x, dialogId6, createdAtBase.AddMinutes(6)))
            .ExecuteAndAssert(_ => { });

        var orderBy = OrderSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse("createdAt_asc", out var orderSet)
            ? orderSet
            : throw new InvalidOperationException("Unable to parse createdAt order.");

        var firstPage = await FlowBuilder.For(Application)
            .SearchEndUserDialogs(x =>
            {
                x.Party = [TestUsers.DefaultParty];
                x.ServiceResource = [DummyService];
                x.Limit = 2;
                x.OrderBy = orderBy;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>();

        firstPage.Items.Should().HaveCount(2);
        firstPage.Items.Select(x => x.Id).Should().Equal([dialogId1, dialogId2]);
        firstPage.HasNextPage.Should().BeTrue();
        firstPage.ContinuationToken.Should().NotBeNullOrWhiteSpace();

        var continuationToken = ContinuationTokenSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse(
            firstPage.ContinuationToken,
            out var parsedToken)
            ? parsedToken
            : throw new InvalidOperationException("Unable to parse continuation token.");

        var secondPage = await FlowBuilder.For(Application)
            .SearchEndUserDialogs(x =>
            {
                x.Party = [TestUsers.DefaultParty];
                x.ServiceResource = [DummyService];
                x.Limit = 2;
                x.OrderBy = orderBy;
                x.ContinuationToken = continuationToken;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>();

        secondPage.Items.Should().HaveCount(2);
        secondPage.Items.Select(x => x.Id).Should().Equal([dialogId3, dialogId4]);
        secondPage.Items.Select(x => x.Id).Should().NotIntersectWith(firstPage.Items.Select(x => x.Id));
        secondPage.HasNextPage.Should().BeTrue();
    }

    private static void ConfDialog(CreateDialogCommand command, Guid dialogId, DateTimeOffset createdAt)
    {
        command.Dto.Id = dialogId;
        command.Dto.Party = TestUsers.DefaultParty;
        command.Dto.ServiceResource = DummyService;
        command.Dto.CreatedAt = createdAt;
    }

    [Fact]
    public async Task Search_Dialogs_As_Email_User() =>
        // IntegrationEmailUser have this email: TEST@TEST.NO
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = IdportenEmailUserIdentifier.PrefixWithSeparator + "Test@Test.no";
            })
            .AsIntegrationEmailUser()
            .SearchEndUserDialogs(x =>
            {
                x.Party = [IdportenEmailUserIdentifier.PrefixWithSeparator + "test@test.NO"];
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().ContainSingle());

    [Fact]
    public async Task Search_Dialogs_As_Legacy_SI_User() =>
        // IntegrationLegacySIUser have this username: UserName
        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = AltinnSelfIdentifiedUserIdentifier.PrefixWithSeparator + "USERNAME";
            })
            .AsIntegrationlegacySIUser()
            .SearchEndUserDialogs(x =>
            {
                x.Party = [AltinnSelfIdentifiedUserIdentifier.PrefixWithSeparator + "uSeRnAmE"];
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().ContainSingle());
}
