using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;


[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchSnapshotTests : ApplicationCollectionFixture
{
    public SearchSnapshotTests(DialogApplication application) : base(application) { }

    private const string ServiceResource = "urn:altinn:resource:1337";

    [Fact]
    public async Task Search_Dialog_Verify_Output()
    {
        var searchResult = await FlowBuilder.For(Application)
            .CreateComplexDialog(x =>
            {
                x.Dto.Activities.Clear();
                x.Dto.ServiceResource = ServiceResource;
            })
            .GetEndUserDialog() // Trigger seen log
            .CreateComplexDialog(x =>
            {
                x.Dto.Attachments.Clear();
                x.Dto.ServiceResource = ServiceResource;
            })
            .CreateComplexDialog(x =>
            {
                x.Dto.Transmissions.Clear();
                x.Dto.ServiceResource = ServiceResource;
            })
            .CreateComplexDialog(x =>
            {
                x.Dto.SystemLabel = SystemLabel.Values.Archive;
                x.Dto.ServiceResource = ServiceResource;
            })
            .CreateComplexDialog(x =>
            {
                x.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = "some-label" }];
                x.Dto.ServiceResource = ServiceResource;
            })
            .SearchEndUserDialogs(x => { x.ServiceResource = [ServiceResource]; })
            .ExecuteAndAssert<PaginatedList<DialogDto>>();

        var settings = new VerifySettings();

        // Timestamps and tiebreaker UUIDs on continuation token will differ on each run
        settings.IgnoreMember(nameof(PaginatedList<DialogDto>.ContinuationToken));

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

        await Verify(searchResult, settings)
            .UseDirectory("Snapshots");
    }
}

