using System.Globalization;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using AwesomeAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search.FilterTests;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ExpiresAtFilterTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Theory, ClassData(typeof(ExpiresAtTestData))]
    public async Task Should_Filter_On_ExpiresAt(ExpiresAtScenario scenario)
    {
        var createDialogCommands = scenario.Dialogs
            .Select(CreateDialogCommand)
            .ToArray();

        var creationTime = createDialogCommands.Max(x => x.Dto.CreatedAt)!.Value;

        await FlowBuilder.For(Application)
            .OverrideUtc(creationTime)
            .CreateDialogs(createDialogCommands)
            .OverrideUtc(scenario.ExpiresAtBefore)
            .SearchEndUserDialogs(x => x.Party = [Tests.Common.Common.Party])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result => result.Items
                .Select(x => x.ExternalReference)
                .Should()
                .BeEquivalentTo(scenario.ExpectedServiceResources));
    }

    private static CreateDialogCommand CreateDialogCommand(DialogData data)
    {
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(data.Id);
        createDialogCommand.Dto.Party = Tests.Common.Common.Party;
        createDialogCommand.Dto.ExternalReference = data.Reference;
        createDialogCommand.Dto.CreatedAt = data.CreatedAt;
        createDialogCommand.Dto.UpdatedAt = data.CreatedAt;
        createDialogCommand.Dto.ExpiresAt = data.ExpiresAt;
        createDialogCommand.Dto.DueAt = data.ExpiresAt.AddDays(-1);
        return createDialogCommand;
    }

    public sealed record ExpiresAtScenario(
        string DisplayName,
        List<DialogData> Dialogs,
        DateTimeOffset ExpiresAtBefore,
        List<string> ExpectedServiceResources) : ClassDataBase(DisplayName);

    private sealed class ExpiresAtTestData : TheoryData<ExpiresAtScenario>
    {
        private static readonly DateTimeOffset Jan1 = DateTimeOffset.Parse("2020-01-01T10:00:00Z", CultureInfo.InvariantCulture);
        private static readonly DateTimeOffset Feb1 = DateTimeOffset.Parse("2020-02-01T10:00:00Z", CultureInfo.InvariantCulture);

        public ExpiresAtTestData()
        {
            var dialogDataSet = Enumerable.Range(0, 10)
                .Select(i => new DialogData(
                    Id: Tests.Common.Common.NewUuidV7(Jan1.AddDays(i)),
                    Reference: i.ToString(CultureInfo.InvariantCulture),
                    CreatedAt: Jan1.AddDays(i),
                    ExpiresAt: Feb1.AddDays(i)))
                .ToList();

            Add(new ExpiresAtScenario(
                DisplayName: "ExpiresAt before 1 hour earlier: all dialogs",
                Dialogs: dialogDataSet,
                ExpiresAtBefore: Feb1.AddHours(-1),
                ExpectedServiceResources: ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"]));

            Add(new ExpiresAtScenario(
                DisplayName: "ExpiresAt before Feb1: except first dialog",
                Dialogs: dialogDataSet,
                ExpiresAtBefore: Feb1,
                ExpectedServiceResources: ["1", "2", "3", "4", "5", "6", "7", "8", "9"]));

            Add(new ExpiresAtScenario(
                DisplayName: "ExpiresAt before Feb1 + 4 days: last five dialogs",
                Dialogs: dialogDataSet,
                ExpiresAtBefore: Feb1.AddDays(4),
                ExpectedServiceResources: ["5", "6", "7", "8", "9"]));

            Add(new ExpiresAtScenario(
                DisplayName: "ExpiresAt before Feb1 + 8 days: last dialog",
                Dialogs: dialogDataSet,
                ExpiresAtBefore: Feb1.AddDays(8),
                ExpectedServiceResources: ["9"]));

            Add(new ExpiresAtScenario(
                DisplayName: "ExpiresAt before Feb1 + 14 days: none",
                Dialogs: dialogDataSet,
                ExpiresAtBefore: Feb1.AddDays(14),
                ExpectedServiceResources: []));
        }
    }

    public sealed record DialogData(Guid Id, string Reference, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt);
}
