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
public class CreatedAtFilterTests : ApplicationCollectionFixture
{
    public CreatedAtFilterTests(DialogApplication application) : base(application) { }

    [Theory]
    [InlineData(2022, null, 2, new[] { 2022, 2023 })]
    [InlineData(null, 2021, 2, new[] { 2020, 2021 })]
    [InlineData(2021, 2022, 2, new[] { 2021, 2022 })]
    public async Task Should_Filter_On_Created_Date(int? createdAfterYear, int? createdBeforeYear, int expectedCount, int[] expectedYears)
    {
        var dialogId2020 = NewUuidV7();
        var dialogId2021 = NewUuidV7();
        var dialogId2022 = NewUuidV7();
        var dialogId2023 = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateDialog(WithCreatedAtInYear(2020, dialogId2020))
            .CreateDialog(WithCreatedAtInYear(2021, dialogId2021))
            .CreateDialog(WithCreatedAtInYear(2022, dialogId2022))
            .CreateDialog(WithCreatedAtInYear(2023, dialogId2023))
            .SearchEndUserDialogs(x =>
            {
                x.Party = [Party];
                x.CreatedAfter = createdAfterYear.HasValue ? CreateDateFromYear(createdAfterYear.Value) : null;
                x.CreatedBefore = createdBeforeYear.HasValue ? CreateDateFromYear(createdBeforeYear.Value) : null;
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

    private static CreateDialogCommand WithCreatedAtInYear(int year, Guid dialogId) => DialogGenerator
        .GenerateFakeCreateDialogCommand(id: dialogId, party: Party, createdAt: CreateDateFromYear(year));

    [Fact]
    public Task Cannot_Filter_On_Created_After_With_Value_Greater_Than_Created_Before() =>
        FlowBuilder.For(Application)
            .SearchEndUserDialogs(x =>
            {
                x.Party = [Party];
                x.CreatedAfter = CreateDateFromYear(2022);
                x.CreatedBefore = CreateDateFromYear(2021);
            })
            .ExecuteAndAssert<ValidationError>(result => result.Should().NotBeNull());
}
