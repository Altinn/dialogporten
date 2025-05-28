using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed class DynamicDateFilterTestData : TheoryData<int?, int?, int, int[]>
{
    // The numbers added to "currentYear" here represent future years relative to the current year.
    // This is done to create test data for dialogs that are due or visible "soon" (1 to 4 years ahead).
    // This approach ensures that the tests remain valid and relevant regardless of the current date.
    public DynamicDateFilterTestData()
    {
        var currentYear = DateTimeOffset.UtcNow.Year;

        // AfterYear, BeforeYear, ExpectedCount, ExpectedYears
        Add(currentYear + 3, null, 2, [currentYear + 3, currentYear + 4]);
        Add(null, currentYear + 2, 2, [currentYear + 1, currentYear + 2]);
        Add(currentYear + 1, currentYear + 2, 2, [currentYear + 1, currentYear + 2]);
    }
}

internal static class Common
{
    internal static DateTimeOffset CreateDateFromYear(int year) => new(year, 1, 1, 0, 0, 0, TimeSpan.Zero);

    internal const string UpdatedAt = "UpdatedAt";
    internal const string VisibleFrom = "VisibleFrom";
    internal const string DueAt = "DueAt";
    internal const string CreatedAt = "CreatedAt";

    // Any party will do, required for EndUser search validation
    internal static string Party => NorwegianPersonIdentifier.PrefixWithSeparator + "03886595947";

    internal static Guid NewUuidV7() => IdentifiableExtensions.CreateVersion7();

}

internal static class ApplicationExtensions
{
    internal static async Task<Guid> CreateDialogWithDateInYear(this DialogApplication application, int year, string dateType)
    {
        var date = CreateDateFromYear(year);
        var createDialogCommand = dateType switch
        {
            UpdatedAt => DialogGenerator.GenerateFakeCreateDialogCommand(
                // Requires CreatedAt to be earlier than UpdatedAt
                createdAt: CreateDateFromYear(year - 1), updatedAt: date),

            VisibleFrom => DialogGenerator.GenerateFakeCreateDialogCommand(
                // Requires DueAt to be later than VisibleFrom
                dueAt: CreateDateFromYear(year + 1), visibleFrom: date),

            DueAt => DialogGenerator.GenerateFakeCreateDialogCommand(dueAt: date),
            CreatedAt => DialogGenerator.GenerateFakeCreateDialogCommand(createdAt: date),
            _ => throw new ArgumentException("Invalid date type", nameof(dateType))
        };

        createDialogCommand.Dto.Party = Party;

        var createCommandResponse = await application.Send(createDialogCommand);
        return createCommandResponse.AsT0.DialogId;
    }
}
