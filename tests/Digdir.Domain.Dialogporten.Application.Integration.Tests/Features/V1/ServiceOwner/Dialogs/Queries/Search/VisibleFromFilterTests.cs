using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class VisibleFromFilterTests : ApplicationCollectionFixture
{
    public VisibleFromFilterTests(DialogApplication application) : base(application) { }

    [Theory]
    [InlineData(2022, null, 2, new[] { 2022, 2023 })]
    [InlineData(null, 2021, 2, new[] { 2020, 2021 })]
    [InlineData(2021, 2022, 2, new[] { 2021, 2022 })]
    public async Task Should_Filter_On_VisibleFrom_Date(int? visibleFromAfterYear, int? visibleFromBeforeYear, int expectedCount, int[] expectedYears)
    {
        // Arrange
        var dialogIn2020 = await CreateDialogWithVisibleFromInYear(2020);
        var dialogIn2021 = await CreateDialogWithVisibleFromInYear(2021);
        var dialogIn2022 = await CreateDialogWithVisibleFromInYear(2022);
        var dialogIn2023 = await CreateDialogWithVisibleFromInYear(2023);

        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            VisibleAfter = visibleFromAfterYear.HasValue ? CreateDateFromYear(visibleFromAfterYear.Value) : null,
            VisibleBefore = visibleFromBeforeYear.HasValue ? CreateDateFromYear(visibleFromBeforeYear.Value) : null
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
    public async Task Cannot_Filter_On_VisibleFrom_After_With_Value_Greater_Than_VisibleFrom_Before()
    {
        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            VisibleAfter = CreateDateFromYear(2022),
            VisibleBefore = CreateDateFromYear(2021)
        });

        // Assert
        response.TryPickT1(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
    }

    private async Task<Guid> CreateDialogWithVisibleFromInYear(int year)
    {
        var visibleFrom = CreateDateFromYear(year);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(visibleFrom: visibleFrom);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return createCommandResponse.AsT0.DialogId;
    }
}
