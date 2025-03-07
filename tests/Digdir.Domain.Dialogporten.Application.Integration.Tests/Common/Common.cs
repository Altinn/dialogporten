using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed class DateFilterTestData
{
    public int? AfterYear { get; init; }
    public int? BeforeYear { get; init; }
    public int ExpectedCount { get; init; }
    public required int[] ExpectedYears { get; init; }
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
