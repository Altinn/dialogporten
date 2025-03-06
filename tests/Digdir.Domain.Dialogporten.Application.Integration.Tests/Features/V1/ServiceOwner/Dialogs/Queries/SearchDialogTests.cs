using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogTests : ApplicationCollectionFixture
{
    public SearchDialogTests(DialogApplication application) : base(application) { }

    [Theory]
    [InlineData(2022, null, 2, new[] { 2022, 2023 })]
    [InlineData(null, 2021, 2, new[] { 2020, 2021 })]
    [InlineData(2021, 2022, 2, new[] { 2021, 2022 })]
    public async Task Should_Filter_On_Created_Date(int? createdAfterYear, int? createdBeforeYear, int expectedCount, int[] expectedYears)
    {
        // Arrange
        var dialogIn2020 = await CreateDialogWithCreatedAtInYear(2020);
        var dialogIn2021 = await CreateDialogWithCreatedAtInYear(2021);
        var dialogIn2022 = await CreateDialogWithCreatedAtInYear(2022);
        var dialogIn2023 = await CreateDialogWithCreatedAtInYear(2023);

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

    private static DateTimeOffset CreateDateFromYear(int year) => new(year, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private async Task<Guid> CreateDialogWithCreatedAtInYear(int year)
    {
        var createdAt = CreateDateFromYear(year);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(createdAt: createdAt);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return createCommandResponse.AsT0.DialogId;
    }

    private async Task<Guid> CreateDialogWithUpdatedAtInYear(int year)
    {
        var updatedAt = CreateDateFromYear(year);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(updatedAt: updatedAt);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return createCommandResponse.AsT0.DialogId;
    }

    private async Task<Guid> CreateDialogWithDueAtInYear(int year)
    {
        var dueAt = CreateDateFromYear(year);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(dueAt: dueAt);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return createCommandResponse.AsT0.DialogId;
    }

    private async Task<Guid> CreateDialogWithExpiresAtInYear(int year)
    {
        var expiresAt = CreateDateFromYear(year);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(expiresAt: expiresAt);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return createCommandResponse.AsT0.DialogId;
    }

    private async Task<Guid> CreateDialogWithVisibleFromInYear(int year)
    {
        var visibleFrom = CreateDateFromYear(year);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(visibleFrom: visibleFrom);
        var createCommandResponse = await Application.Send(createDialogCommand);
        return createCommandResponse.AsT0.DialogId;
    }
}
