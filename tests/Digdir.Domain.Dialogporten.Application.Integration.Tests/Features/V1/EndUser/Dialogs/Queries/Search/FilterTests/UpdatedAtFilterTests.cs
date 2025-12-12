using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using DialogDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.DialogDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search.FilterTests;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdatedAtFilterTests : ApplicationCollectionFixture
{
    public UpdatedAtFilterTests(DialogApplication application) : base(application) { }

    [Theory]
    [InlineData(2022, null, new[] { 2022, 2023 })]
    [InlineData(null, 2021, new[] { 2020, 2021 })]
    [InlineData(2021, 2022, new[] { 2021, 2022 })]
    public async Task Should_Filter_On_Updated_Date(int? updatedAfterYear, int? updatedBeforeYear, int[] expectedYears)
    {
        var expectedDialogIds = new List<Guid>();

        var createDialogCommands = Enumerable
            .Range(2020, 4)
            .Select(year =>
            {
                var dialogId = NewUuidV7();

                if (expectedYears.Contains(year))
                {
                    expectedDialogIds.Add(dialogId);
                }

                return CreateDialogCommand(year, dialogId);
            }).ToArray();

        await FlowBuilder.For(Application)
            .CreateDialogs(createDialogCommands)
            .SearchEndUserDialogs(x =>
            {
                x.Party = [Party];
                x.UpdatedAfter = updatedAfterYear.HasValue ? CreateDateFromYear(updatedAfterYear.Value) : null;
                x.UpdatedBefore = updatedBeforeYear.HasValue ? CreateDateFromYear(updatedBeforeYear.Value) : null;
            })
            .ExecuteAndAssert<PaginatedList<DialogDto>>(result =>
            {
                result.Items
                    .Select(x => x.Id)
                    .Should()
                    .BeEquivalentTo(expectedDialogIds);
            });
    }

    private static CreateDialogCommand CreateDialogCommand(int year, Guid dialogId) =>
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
