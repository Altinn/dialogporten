using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

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
        // Arrange
        var dialogIn2020 = await Application.CreateDialogWithDateInYear(2020, CreatedAt);
        var dialogIn2021 = await Application.CreateDialogWithDateInYear(2021, CreatedAt);
        var dialogIn2022 = await Application.CreateDialogWithDateInYear(2022, CreatedAt);
        var dialogIn2023 = await Application.CreateDialogWithDateInYear(2023, CreatedAt);

        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            CreatedAfter = createdAfterYear.HasValue ? CreateDateFromYear(createdAfterYear.Value) : null,
            CreatedBefore = createdBeforeYear.HasValue ? CreateDateFromYear(createdBeforeYear.Value) : null
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
    public async Task Cannot_Filter_On_Created_After_With_Value_Greater_Than_Created_Before()
    {
        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            CreatedAfter = CreateDateFromYear(2022),
            CreatedBefore = CreateDateFromYear(2021)
        });

        // Assert
        response.TryPickT1(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
    }
}
