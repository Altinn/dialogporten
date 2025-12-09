using System.Globalization;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search.FilterTests;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ExpiresAtFilterTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Theory]
    [ClassData(typeof(DialogVisibleFromFilterTestData))]
    public async Task Should_Filter_On_ExpiresAt(
        List<DialogData> dataSet,
        DateTimeOffset searchTime,
        List<string> expectedReferences)
    {
        var createDialogCommands = dataSet
            .Select(CreateDialogCommand)
            .ToArray();
        var creationTime = createDialogCommands.Max(x => x.Dto.CreatedAt)!.Value;
        await FlowBuilder.For(Application)
            .OverrideUtc(creationTime)
            .CreateDialogs(createDialogCommands)
            .OverrideUtc(searchTime)
            .SearchEndUserDialogs(x => x.Party = [Tests.Common.Common.Party])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
            {
                result.Items
                    .Select(x => x.ExternalReference)
                    .Should()
                    .BeEquivalentTo(expectedReferences);
            });
    }

    private static CreateDialogCommand CreateDialogCommand(DialogData data) =>
        DialogGenerator.GenerateFakeCreateDialogCommand(
            id: data.Id,
            party: Tests.Common.Common.Party,
            externalReference: data.Reference,
            createdAt: data.CreatedAt,
            updatedAt: data.CreatedAt,
            expiresAt: data.ExpiresAt,
            dueAt: data.ExpiresAt.AddDays(-1),
            activities: [],
            transmissions: []);

    private sealed class DialogVisibleFromFilterTestData : TheoryData<List<DialogData>, DateTimeOffset, List<string>>
    {
        private static readonly DateTimeOffset Jan1 = DateTimeOffset.Parse("2020-01-01T10:00:00Z", CultureInfo.InvariantCulture);
        private static readonly DateTimeOffset Feb1 = DateTimeOffset.Parse("2020-02-01T10:00:00Z", CultureInfo.InvariantCulture);

        public DialogVisibleFromFilterTestData()
        {
            var dialogDataSet = Enumerable.Range(0, 10)
                .Select(i => new DialogData(
                    Id: Tests.Common.Common.NewUuidV7(),
                    Reference: i.ToString(CultureInfo.InvariantCulture),
                    CreatedAt: Jan1.AddDays(i),
                    ExpiresAt: Feb1.AddDays(i)))
                .ToList();
            // DataSet, SearchTime, ExpectedReferences
            Add(dialogDataSet, Feb1.AddHours(-1), ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"]);
            Add(dialogDataSet, Feb1, ["1", "2", "3", "4", "5", "6", "7", "8", "9"]);
            Add(dialogDataSet, Feb1.AddDays(4), ["5", "6", "7", "8", "9"]);
            Add(dialogDataSet, Feb1.AddDays(8), ["9"]);
            Add(dialogDataSet, Feb1.AddDays(14), []);
        }
    }

    public sealed record DialogData(Guid Id, string Reference, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
}
