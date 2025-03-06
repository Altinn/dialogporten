using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

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
        // Arrange
        var dialogIn2020 = await CreateDialogWithUpdatedAtInYear(2020);
        var dialogIn2021 = await CreateDialogWithUpdatedAtInYear(2021);
        var dialogIn2022 = await CreateDialogWithUpdatedAtInYear(2022);
        var dialogIn2023 = await CreateDialogWithUpdatedAtInYear(2023);

        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            UpdatedAfter = updatedAfterYear.HasValue ? CreateDateFromYear(updatedAfterYear.Value) : null,
            UpdatedBefore = updatedBeforeYear.HasValue ? CreateDateFromYear(updatedBeforeYear.Value) : null
        });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();

        result.Items.Should().HaveCount(expectedCount);
        foreach (var year in expectedYears)
        {
            var dialogId = year switch
            {
                2020 => dialogIn2020,
                2021 => dialogIn2021,
                2022 => dialogIn2022,
                2023 => dialogIn2023,
                _ => throw new ArgumentOutOfRangeException()
            };

            result.Items.Should().ContainSingle(x => x.Id == dialogId);
        }
    }

    [Fact]
    public async Task Cannot_Filter_On_UpdatedAfter_With_Value_Greater_Than_UpdatedBefore()
    {
        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            UpdatedAfter = CreateDateFromYear(2022),
            UpdatedBefore = CreateDateFromYear(2021)
        });

        // Assert
        response.TryPickT1(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
    }

    private async Task<Guid> CreateDialogWithUpdatedAtInYear(int year)
    {
        var createdAt = CreateDateFromYear(year - 1);
        var updatedAt = CreateDateFromYear(year);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(createdAt: createdAt, updatedAt: updatedAt);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return createCommandResponse.AsT0.DialogId;
    }
}
