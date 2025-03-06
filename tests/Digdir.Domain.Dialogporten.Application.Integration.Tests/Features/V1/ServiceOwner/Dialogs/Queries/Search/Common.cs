namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

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
}
