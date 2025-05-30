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
    public DueAtFilterTests(DialogApplication application) : base(application)
    {
    }

    [Theory, ClassData(typeof(DynamicDateFilterTestData))]
    public async Task Should_Filter_On_Due_Date(int? afterYear, int? beforeYear, int expectedCount, int[] expectedYears)
    {
        var currentYear = DateTimeOffset.UtcNow.Year;

        // var oneYearInTheFuture = currentYear + 1;
        var twoYearsInTheFuture = currentYear + 2;
        var threeYearsInTheFuture = currentYear + 3;
        var fourYearsInTheFuture = currentYear + 4;

        var dialogIdOneYearInTheFuture = NewUuidV7();
        var dialogIdTwoYearsInTheFuture = NewUuidV7();
        var dialogIdThreeYearsInTheFuture = NewUuidV7();
        var dialogIdFourYearsInTheFuture = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateDialog(WithDueAtInYear(2020, dialogIdOneYearInTheFuture))
            .CreateDialog(WithDueAtInYear(twoYearsInTheFuture, dialogIdTwoYearsInTheFuture))
            .CreateDialog(WithDueAtInYear(threeYearsInTheFuture, dialogIdThreeYearsInTheFuture))
            .CreateDialog(WithDueAtInYear(fourYearsInTheFuture, dialogIdFourYearsInTheFuture))
            .SearchServiceOwnerDialogs(x =>
            {
                x.DueAfter = afterYear.HasValue ? CreateDateFromYear(afterYear.Value) : null;
                x.DueBefore = beforeYear.HasValue ? CreateDateFromYear(beforeYear.Value) : null;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
            {
                result.Items.Should().HaveCount(expectedCount);

                foreach (var year in expectedYears)
                {
                    var dialogId = year switch
                    {
                        _ when year == 2020 => dialogIdOneYearInTheFuture,
                        _ when year == twoYearsInTheFuture => dialogIdTwoYearsInTheFuture,
                        _ when year == threeYearsInTheFuture => dialogIdThreeYearsInTheFuture,
                        _ when year == fourYearsInTheFuture => dialogIdFourYearsInTheFuture,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    result.Items.Should().ContainSingle(x => x.Id == dialogId);
                }
            });
    }

    private static CreateDialogCommand WithDueAtInYear(int year, Guid dialogId) =>
        DialogGenerator.GenerateFakeCreateDialogCommand(id: dialogId, dueAt: CreateDateFromYear(year));

    [Fact]
    public Task Cannot_Filter_On_DueAfter_With_Value_Greater_Than_DueBefore() =>
        FlowBuilder.For(Application)
            .SearchServiceOwnerDialogs(x =>
            {
                x.DueAfter = CreateDateFromYear(2022);
                x.DueBefore = CreateDateFromYear(2021);
            })
            .ExecuteAndAssert<ValidationError>(result => result.Should().NotBeNull());
}
