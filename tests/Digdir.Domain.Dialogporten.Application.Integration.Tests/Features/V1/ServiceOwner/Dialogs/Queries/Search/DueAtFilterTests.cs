using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DueAtFilterTests : ApplicationCollectionFixture
{
    public DueAtFilterTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Cannot_Filter_On_DueAfter_With_Value_Greater_Than_DueBefore()
    {
        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            DueAfter = CreateDateFromYear(2022),
            DueBefore = CreateDateFromYear(2021)
        });

        // Assert
        response.TryPickT1(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
    }

    [Theory, MemberData(nameof(GetDueAtTestData))]
    public async Task Should_Filter_On_Due_Date(DateFilterTestData testData)
    {
        // Arrange
        var currentYear = DateTimeOffset.UtcNow.Year;

        var oneYearInTheFuture = currentYear + 1;
        var twoYearsInTheFuture = currentYear + 2;
        var threeYearsInTheFuture = currentYear + 3;
        var fourYearsInTheFuture = currentYear + 4;

        var dialogOneYearInTheFuture = await CreateDialogWithDueAtInYear(oneYearInTheFuture);
        var dialogTwoYearsInTheFuture = await CreateDialogWithDueAtInYear(twoYearsInTheFuture);
        var dialogThreeYearsInTheFuture = await CreateDialogWithDueAtInYear(threeYearsInTheFuture);
        var dialogFourYearsInTheFuture = await CreateDialogWithDueAtInYear(fourYearsInTheFuture);

        // Act
        var response = await Application.Send(new SearchDialogQuery
        {
            DueAfter = testData.AfterYear.HasValue ? CreateDateFromYear(testData.AfterYear.Value) : null,
            DueBefore = testData.BeforeYear.HasValue ? CreateDateFromYear(testData.BeforeYear.Value) : null
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

    public static IEnumerable<object[]> GetDueAtTestData()
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

    private async Task<Guid> CreateDialogWithDueAtInYear(int year)
    {
        var dueAt = CreateDateFromYear(year);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(dueAt: dueAt);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return createCommandResponse.AsT0.DialogId;
    }
}
