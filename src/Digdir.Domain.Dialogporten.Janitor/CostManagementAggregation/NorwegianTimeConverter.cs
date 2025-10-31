namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

/// <summary>
/// Utility class for converting between UTC and Norwegian (Europe/Oslo) timezone.
/// Handles both CET (UTC+1) and CEST (UTC+2) depending on daylight saving time.
/// </summary>
public static class NorwegianTimeConverter
{
    private static readonly TimeZoneInfo NorwegianTimeZone = GetNorwegianTimeZone();

    private static TimeZoneInfo GetNorwegianTimeZone()
    {
        // Try IANA timezone identifier first (Linux/macOS)
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Oslo");
        }
        catch (TimeZoneNotFoundException)
        {
            // Fall back to Windows timezone identifier
            return TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
        }
    }

    /// <summary>
    /// Gets yesterday's date in Norwegian timezone.
    /// </summary>
    /// <returns>DateOnly representing yesterday in Norwegian time</returns>
    public static DateOnly GetYesterday()
    {
        var norwegianNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, NorwegianTimeZone);
        return DateOnly.FromDateTime(norwegianNow.AddDays(-1));
    }

    /// <summary>
    /// Converts a Norwegian date to a UTC time range covering the entire day.
    /// </summary>
    /// <param name="norwegianDate">The date in Norwegian timezone</param>
    /// <returns>Tuple containing start and end of day in UTC</returns>
    public static (DateTime startUtc, DateTime endUtc) GetDayRangeInUtc(DateOnly norwegianDate) =>
    (
        TimeZoneInfo.ConvertTimeToUtc(norwegianDate.ToDateTime(TimeOnly.MinValue), NorwegianTimeZone),
        TimeZoneInfo.ConvertTimeToUtc(norwegianDate.ToDateTime(TimeOnly.MaxValue), NorwegianTimeZone)
    );
}
