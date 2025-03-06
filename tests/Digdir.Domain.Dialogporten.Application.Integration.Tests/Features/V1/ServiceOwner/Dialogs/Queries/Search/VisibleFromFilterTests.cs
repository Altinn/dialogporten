using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class VisibleFromFilterTests : ApplicationCollectionFixture
{
    public VisibleFromFilterTests(DialogApplication application) : base(application) { }

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

    [Theory, MemberData(nameof(GetVisibleFromTestData))]
    public async Task Should_Filter_On_VisibleFrom_Date(DateFilterTestData testData)
    {
        // Arrange
        var currentYear = DateTimeOffset.UtcNow.Year;

        var oneYearInTheFuture = currentYear + 1;
        var twoYearsInTheFuture = currentYear + 2;
        var threeYearsInTheFuture = currentYear + 3;
        var fourYearsInTheFuture = currentYear + 4;

        var dialogOneYearInTheFuture = await Application.CreateDialogWithDateInYear(oneYearInTheFuture, VisibleFrom);
        var dialogTwoYearsInTheFuture = await Application.CreateDialogWithDateInYear(twoYearsInTheFuture, VisibleFrom);
        var dialogThreeYearsInTheFuture = await Application.CreateDialogWithDateInYear(threeYearsInTheFuture, VisibleFrom);
        var dialogFourYearsInTheFuture = await Application.CreateDialogWithDateInYear(fourYearsInTheFuture, VisibleFrom);

        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            VisibleAfter = testData.AfterYear.HasValue ? CreateDateFromYear(testData.AfterYear.Value) : null,
            VisibleBefore = testData.BeforeYear.HasValue ? CreateDateFromYear(testData.BeforeYear.Value) : null
        });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();

        result.Items.Should().HaveCount(testData.ExpectedCount);
        foreach (var year in testData.ExpectedYears)
        {
            var dialogId = year switch
            {
                _ when year == oneYearInTheFuture => dialogOneYearInTheFuture,
                _ when year == twoYearsInTheFuture => dialogTwoYearsInTheFuture,
                _ when year == threeYearsInTheFuture => dialogThreeYearsInTheFuture,
                _ when year == fourYearsInTheFuture => dialogFourYearsInTheFuture,
                _ => throw new ArgumentOutOfRangeException()
            };

            result.Items.Should().ContainSingle(x => x.Id == dialogId);
        }
    }

    public static IEnumerable<object[]> GetVisibleFromTestData()
    {
        var currentYear = DateTimeOffset.UtcNow.Year;
        return new List<object[]>
        {
            new object[]
            {
                new DateFilterTestData
                {
                    AfterYear = currentYear + 3,
                    BeforeYear = null,
                    ExpectedCount = 2,
                    ExpectedYears = [currentYear + 3, currentYear + 4]
                }
            },
            new object[]
            {
                new DateFilterTestData
                {
                    AfterYear = null,
                    BeforeYear = currentYear + 2,
                    ExpectedCount = 2,
                    ExpectedYears = [currentYear + 1, currentYear + 2]
                }
            },
            new object[]
            {
                new DateFilterTestData
                {
                    AfterYear = currentYear + 1,
                    BeforeYear = currentYear + 2,
                    ExpectedCount = 2,
                    ExpectedYears = [currentYear + 1, currentYear + 2]
                }
            }
        };
    }
}
