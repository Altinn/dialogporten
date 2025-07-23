using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Parties;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Search_Should_Populate_EnduserContextRevision()
    {
        string? party = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => party = x.Dto.Party)
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
            .CreateSimpleDialog(x => party = x.Dto.Party)
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
            .CreateSimpleDialog(x =>
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
            .CreateSimpleDialog(x => x
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
                                { IntegrationTestUser.DefaultParty, [DummyService] }
                            },
                            // Delegated dialog
                            DialogIds = [delegatedDialogId]
                        });
                });
            })
            .CreateSimpleDialog(x =>
            {
                // Delegated dialog
                x.Dto.ServiceResource = DummyService;
                x.Dto.Id = delegatedDialogId;
                x.Dto.Party = delegatedDialogParty;
            })
            .CreateSimpleDialog(x =>
            {
                // Default integration test user dialog
                x.Dto.ServiceResource = DummyService;
                x.Dto.Party = IntegrationTestUser.DefaultParty;
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
                    d.Party == IntegrationTestUser.DefaultParty);
            });
    }
}
