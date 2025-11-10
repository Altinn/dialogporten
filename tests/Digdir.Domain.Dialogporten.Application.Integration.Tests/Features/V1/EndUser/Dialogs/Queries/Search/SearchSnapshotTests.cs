using System.Runtime.CompilerServices;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;


[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchSnapshotTests : ApplicationCollectionFixture
{
    public SearchSnapshotTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Search_Dialog_Verify_Output()
    {
        var searchResult = await FlowBuilder.For(Application)
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Activities.Clear();
            })
            .GetEndUserDialog() // Trigger seen log
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Attachments.Clear();
            })
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.Transmissions.Clear();
            })
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
            })
            .CreateComplexDialog(x =>
            {
                x.Dto = SnapshotDialog.Create();
                x.Dto.ServiceOwnerContext = new DialogServiceOwnerContextDto
                {
                    ServiceOwnerLabels = [new() { Value = "some-label" }]
                };
            })
            .SendCommand(_ => new SearchDialogQuery
            {
                ServiceResource = [SnapshotDialog.ServiceResource],
                OrderBy = OrderSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse("createdAt", out var lala) ? lala : null
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>();

        var settings = new VerifySettings();

        // Timestamps and tiebreaker UUIDs on continuation token will differ on each run
        settings.IgnoreMember(nameof(PaginatedList<DialogDto>.ContinuationToken));

        await Verify(searchResult, settings)
            .UseDirectory("Snapshots");
    }

    [ModuleInitializer]
    internal static void Init()
    {
        // Scrub ephemeral person identifiers that differ on each run
        VerifierSettings.MemberConverter<ActorDto, string>(
            expression: x => x.ActorId,
            converter: x =>
            {
                if (x != null && x.StartsWith(NorwegianPersonIdentifier.HashPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return NorwegianPersonIdentifier.HashPrefix + ":scrubbed";
                }

                return x;
            });
    }
}

