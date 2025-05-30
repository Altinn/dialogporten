using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdatedAtFilterTests : ApplicationCollectionFixture
{
    public UpdatedAtFilterTests(DialogApplication application) : base(application) { }

    [Theory]
    [InlineData(2022, null, 2, new[] { 2022, 2023 })]
    [InlineData(null, 2021, 2, new[] { 2020, 2021 })]
    [InlineData(2021, 2022, 2, new[] { 2021, 2022 })]
    public async Task Should_Filter_On_Updated_Date(int? updatedAfterYear, int? updatedBeforeYear, int expectedCount, int[] expectedYears)
    {
        var dialogId2020 = NewUuidV7();
        var dialogId2021 = NewUuidV7();
        var dialogId2022 = NewUuidV7();
        var dialogId2023 = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateDialog(WithUpdatedAtInYear(2020, dialogId2020))
            .CreateDialog(WithUpdatedAtInYear(2021, dialogId2021))
            .CreateDialog(WithUpdatedAtInYear(2022, dialogId2022))
            .CreateDialog(WithUpdatedAtInYear(2023, dialogId2023))
            .SearchEndUserDialogs(x =>
            {
                x.Party = [Party];
                x.UpdatedAfter = updatedAfterYear.HasValue ? CreateDateFromYear(updatedAfterYear.Value) : null;
                x.UpdatedBefore = updatedBeforeYear.HasValue ? CreateDateFromYear(updatedBeforeYear.Value) : null;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
            {
                result.Items.Should().HaveCount(expectedCount);

                foreach (var year in expectedYears)
                {
                    var dialogId = year switch
                    {
                        2020 => dialogId2020,
                        2021 => dialogId2021,
                        2022 => dialogId2022,
                        2023 => dialogId2023,
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    result.Items.Should().ContainSingle(x => x.Id == dialogId);
                }
            });
    }

    private static CreateDialogCommand WithUpdatedAtInYear(int year, Guid dialogId) =>
        DialogGenerator.GenerateFakeCreateDialogCommand(
            id: dialogId,
            party: Party,
            createdAt: CreateDateFromYear(year - 1), // Requires CreatedAt to be earlier than UpdatedAt
            updatedAt: CreateDateFromYear(year));

    [Fact]
    public Task Cannot_Filter_On_UpdatedAfter_With_Value_Greater_Than_UpdatedBefore() =>
        FlowBuilder.For(Application)
            .SearchEndUserDialogs(x =>
            {
                x.Party = [Party];
                x.UpdatedAfter = CreateDateFromYear(2022);
                x.UpdatedBefore = CreateDateFromYear(2021);
            })
            .ExecuteAndAssert<ValidationError>();
}
