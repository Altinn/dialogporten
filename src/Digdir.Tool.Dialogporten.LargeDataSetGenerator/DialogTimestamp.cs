namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

public record struct DialogTimestamp(DateTimeOffset Timestamp, string FormattedTimestamp, Guid DialogId, int Counter);
