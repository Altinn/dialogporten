using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class VisibleFromFilterTests : ApplicationCollectionFixture
{
    public VisibleFromFilterTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Cannot_Filter_On_VisibleFrom_After_With_Value_Greater_Than_VisibleFrom_Before()
        => FlowBuilder.For(Application)
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.VisibleAfter = CreateDateFromYear(2022);
                x.VisibleBefore = CreateDateFromYear(2021);
            })
            .ExecuteAndAssert<ValidationError>();

    private static CreateDialogCommand CreateDialogCommand(int year, Guid dialogId) => DialogGenerator
        .GenerateFakeCreateDialogCommand(id: dialogId, party: Party,
            visibleFrom: CreateDateFromYear(year),
            dueAt: CreateDateFromYear(year + 1));

    [Theory, ClassData(typeof(DynamicDateFilterTestData))]
    public async Task Should_Filter_On_VisibleFrom_Date(int? afterYear, int? beforeYear, int[] expectedYears)
    {
        var expectedDialogIds = new List<Guid>();

        var createDialogCommands = Enumerable.Range(DateTimeOffset.UtcNow.Year + 1, 4).Select(year =>
        {
            var dialogId = NewUuidV7();
            if (expectedYears.Contains(year))
            {
                expectedDialogIds.Add(dialogId);
            }

            return CreateDialogCommand(year, dialogId);
        }).ToList();

        await FlowBuilder.For(Application)
            .CreateDialogs(createDialogCommands)
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.VisibleAfter = afterYear.HasValue ? CreateDateFromYear(afterYear.Value) : null;
                x.VisibleBefore = beforeYear.HasValue ? CreateDateFromYear(beforeYear.Value) : null;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
            {
                result.Items
                    .Select(x => x.Id)
                    .Should()
                    .BeEquivalentTo(expectedDialogIds);
            });
    }
}
