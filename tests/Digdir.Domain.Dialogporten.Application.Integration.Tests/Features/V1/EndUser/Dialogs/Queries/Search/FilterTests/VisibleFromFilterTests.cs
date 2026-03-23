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
public class VisibleFromFilterTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Theory, ClassData(typeof(VisibleFromTestData))]
    public async Task Should_Filter_On_VisibleFrom(VisibleFromScenario scenario)
    {
        var createDialogCommands = scenario.Dialogs
            .Select(CreateDialogCommand)
            .ToArray();

        var creationTime = createDialogCommands.Max(x => x.Dto.CreatedAt)!.Value;

        await FlowBuilder.For(Application)
            .OverrideUtc(creationTime)
            .CreateDialogs(createDialogCommands)
            .OverrideUtc(scenario.VisibleFrom)
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
        createDialogCommand.Dto.VisibleFrom = data.VisibleFrom;
        return createDialogCommand;
    }

    public sealed record VisibleFromScenario(
        string DisplayName,
        List<DialogData> Dialogs,
        DateTimeOffset VisibleFrom,
        List<string> ExpectedServiceResources) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class VisibleFromTestData : TheoryData<VisibleFromScenario>
    {
        private static readonly DateTimeOffset Jan1 = DateTimeOffset.Parse("2020-01-01T10:00:00Z", CultureInfo.InvariantCulture);
        private static readonly DateTimeOffset Feb1 = DateTimeOffset.Parse("2020-02-01T10:00:00Z", CultureInfo.InvariantCulture);

        public VisibleFromTestData()
        {
            var dialogDataSet = Enumerable.Range(0, 10)
                .Select(i => new DialogData(
                    Id: Tests.Common.Common.NewUuidV7(Jan1.AddDays(1)),
                    Reference: i.ToString(CultureInfo.InvariantCulture),
                    CreatedAt: Jan1.AddDays(i),
                    VisibleFrom: Feb1.AddDays(i)))
                .ToList();

            Add(new VisibleFromScenario(
                DisplayName: "VisibleFrom search before any dialogs",
                Dialogs: dialogDataSet,
                VisibleFrom: Feb1.AddHours(-1),
                ExpectedServiceResources: []));

            Add(new VisibleFromScenario(
                DisplayName: "VisibleFrom on Feb1: first dialog",
                Dialogs: dialogDataSet,
                VisibleFrom: Feb1,
                ExpectedServiceResources: ["0"]));

            Add(new VisibleFromScenario(
                DisplayName: "VisibleFrom before 4 days after Feb1: first five dialogs",
                Dialogs: dialogDataSet,
                VisibleFrom: Feb1.AddDays(4),
                ExpectedServiceResources: ["0", "1", "2", "3", "4"]));

            Add(new VisibleFromScenario(
                DisplayName: "VisibleFrom before 8 days after Feb1: first nine dialogs",
                Dialogs: dialogDataSet,
                VisibleFrom: Feb1.AddDays(8),
                ExpectedServiceResources: ["0", "1", "2", "3", "4", "5", "6", "7", "8"]));

            Add(new VisibleFromScenario(
                DisplayName: "VisibleFrom before 14 days after Feb1: all dialogs",
                Dialogs: dialogDataSet,
                VisibleFrom: Feb1.AddDays(14),
                ExpectedServiceResources: ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"]));
        }
    }

    public sealed record DialogData(Guid Id, string Reference, DateTimeOffset CreatedAt, DateTimeOffset VisibleFrom);
}
