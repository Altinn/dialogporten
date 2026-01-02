using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed class DynamicDateFilterTestData : TheoryData<int?, int?, int[]>
{
    // The numbers added to "currentYear" here represent future years relative to the current year.
    // This is done to create test data for dialogs that are due or visible "soon" (1 to 4 years ahead).
    // This approach ensures that the tests remain valid and relevant regardless of the current date.
    public DynamicDateFilterTestData()
    {
        var currentYear = DateTimeOffset.UtcNow.Year;

        // AfterYear, BeforeYear, ExpectedCount, ExpectedYears
        Add(currentYear + 3, null, [currentYear + 3, currentYear + 4]);
        Add(null, currentYear + 2, [currentYear + 1, currentYear + 2]);
        Add(currentYear + 1, currentYear + 2, [currentYear + 1, currentYear + 2]);
    }
}

internal static class Common
{
    internal static DateTimeOffset CreateDateFromYear(int year) => new(year, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // Any party will do, required for EndUser search validation
    internal static string Party => NorwegianPersonIdentifier.PrefixWithSeparator + "03886595947";

    internal static Guid NewUuidV7(DateTimeOffset? timeStamp = null) => IdentifiableExtensions.CreateVersion7(timeStamp);

    internal static IntegrationTestUser CreateUserWithScope(string scope) => new([new("scope", scope)]);

    internal static Action<IServiceCollection> ConfigureUserWithScope(string scope) => services =>
    {
        var user = CreateUserWithScope(scope);
        services.RemoveAll<IUser>();
        services.AddSingleton<IUser>(user);
    };

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
