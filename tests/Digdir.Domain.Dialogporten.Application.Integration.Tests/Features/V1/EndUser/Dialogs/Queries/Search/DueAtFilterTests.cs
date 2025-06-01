using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DueAtFilterTests : ApplicationCollectionFixture
{
    public DueAtFilterTests(DialogApplication application) : base(application) { }

    [Theory, ClassData(typeof(DynamicDateFilterTestData))]
    public async Task Should_Filter_On_Due_Date(int? afterYear, int? beforeYear, int[] expectedYears)
    {
        var expectedDialogIds = new List<Guid>();

        var createDialogCommands = Enumerable
            // DueAt has to be in the future, so we start from next year
            .Range(DateTimeOffset.UtcNow.Year + 1, 4)
            .Select(year =>
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
            .SearchEndUserDialogs(x =>
            {
                x.Party = [Party];
                x.DueAfter = afterYear.HasValue ? CreateDateFromYear(afterYear.Value) : null;
                x.DueBefore = beforeYear.HasValue ? CreateDateFromYear(beforeYear.Value) : null;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
            {
                result.Items
                    .Select(x => x.Id)
                    .Should()
                    .BeEquivalentTo(expectedDialogIds);
            });
    }

    private static CreateDialogCommand CreateDialogCommand(int year, Guid dialogId)
    {
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Party = Party;
        createDialogCommand.Dto.DueAt = CreateDateFromYear(year);
        createDialogCommand.Dto.Id = dialogId;
        return createDialogCommand;
    }

    [Fact]
    public Task Cannot_Filter_On_DueAfter_With_Value_Greater_Than_DueBefore() =>
        FlowBuilder.For(Application)
            .SearchEndUserDialogs(x =>
            {
                x.Party = [Party];
                x.DueAfter = CreateDateFromYear(2022);
                x.DueBefore = CreateDateFromYear(2021);
            })
            .ExecuteAndAssert<ValidationError>(v => v.ShouldHaveErrorWithText("DueAfter"));
}
