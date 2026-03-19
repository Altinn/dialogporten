using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed record DynamicDateFilterScenario(
    string DisplayName,
    int? DueAtAfterYear,
    int? DueAtBeforeYear,
    int[] ExpectedYears) : ClassDataBase(DisplayName);

public sealed class DynamicDateFilterTestData : TheoryData<DynamicDateFilterScenario>
{
    // The numbers added to "currentYear" here represent future years relative to the current year.
    // This is done to create test data for dialogs that are due or visible "soon" (1 to 4 years ahead).
    // This approach ensures that the tests remain valid and relevant regardless of the current date.
    public DynamicDateFilterTestData()
    {
        var currentYear = DateTimeOffset.UtcNow.Year;

        // AfterYear, BeforeYear, ExpectedCount, ExpectedYears
        Add(new DynamicDateFilterScenario(
            DisplayName: $"DueAtAfter: {currentYear + 3}, DueAtBefore: <empty>, ExpectedYears: [{currentYear + 3},{currentYear + 4}]",
            DueAtAfterYear: currentYear + 3,
            DueAtBeforeYear: null,
            ExpectedYears: [currentYear + 3, currentYear + 4]));
        Add(new DynamicDateFilterScenario(
            DisplayName: $"DueAtAfter: <empty>, DueAtBefore: {currentYear + 2}, ExpectedYears: [{currentYear + 1},{currentYear + 2}]",
            DueAtAfterYear: null,
            DueAtBeforeYear: currentYear + 2,
            ExpectedYears: [currentYear + 1, currentYear + 2]));
        Add(new DynamicDateFilterScenario(
            DisplayName: $"DueAtAfter: {currentYear + 1}, DueAtBefore: {currentYear + 2}, ExpectedYears: [{currentYear + 1},{currentYear + 2}]",
            DueAtAfterYear: currentYear + 1,
            DueAtBeforeYear: currentYear + 2,
            ExpectedYears: [currentYear + 1, currentYear + 2]));
    }
}

internal static class Common
{
    internal static DateTimeOffset CreateDateFromYear(int year) => new(year, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Any party will do, required for EndUser search validation
    internal static string Party => NorwegianPersonIdentifier.PrefixWithSeparator + "03886595947";

    internal static Guid NewUuidV7(DateTimeOffset? timeStamp = null) => IdentifiableExtensions.CreateVersion7(timeStamp);

    internal static ContentValueDto CreateHtmlContentValueDto(string mediaType) => new()
    {
        MediaType = mediaType,
        Value = [new() { LanguageCode = "nb", Value = "<p>Some HTML content</p>" }]
    };

    internal static ContentValueDto CreateEmbeddableHtmlContentValueDto(string mediaType) => new()
    {
        MediaType = mediaType,
        Value = [new() { LanguageCode = "nb", Value = "https://example.html" }]
    };

}
