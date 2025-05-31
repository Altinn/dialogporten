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
public class DueAtFilterTests : ApplicationCollectionFixture
{
    public DueAtFilterTests(DialogApplication application) : base(application) { }

    [Theory, ClassData(typeof(DynamicDateFilterTestData))]
    public async Task Should_Filter_On_Due_Date(int? afterYear, int? beforeYear, int expectedCount, int[] expectedYears)
    {
        var currentYear = DateTimeOffset.UtcNow.Year;
        var expectedDialogIds = new List<Guid>();

        var createDialogCommands = Enumerable.Range(1, 4).Select(x =>
        {
            var dialogId = NewUuidV7();
            var year = currentYear + x;
            if (expectedYears.Contains(year))
            {
                expectedDialogIds.Add(dialogId);
            }
            return CreateDialog(CreateDateFromYear(year), dialogId);
        }).ToList();

        await FlowBuilder.For(Application)
            .CreateDialogs(createDialogCommands)
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.DueAfter = afterYear.HasValue ? CreateDateFromYear(afterYear.Value) : null;
                x.DueBefore = beforeYear.HasValue ? CreateDateFromYear(beforeYear.Value) : null;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
            {
                result.Items.Should().HaveCount(expectedCount);

                result.Items
                    .Select(x => x.Id)
                    .Should()
                    .BeEquivalentTo(expectedDialogIds);
            });
    }

    private static CreateDialogCommand CreateDialog(DateTimeOffset dueAt, Guid dialogId)
    {
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Party = Party;
        createDialogCommand.Dto.DueAt = dueAt;
        createDialogCommand.Dto.Id = dialogId;
        return createDialogCommand;
    }

    [Fact]
    public Task Cannot_Filter_On_DueAfter_With_Value_Greater_Than_DueBefore() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogs(x =>
            {
                x.Party = [Party];
                x.DueAfter = CreateDateFromYear(2022);
                x.DueBefore = CreateDateFromYear(2021);
            })
            .ExecuteAndAssert<ValidationError>(v => v.ShouldHaveErrorWithText("DueAfter"));
}
