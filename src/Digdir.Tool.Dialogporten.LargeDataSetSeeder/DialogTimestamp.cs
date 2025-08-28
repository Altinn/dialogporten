using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

public record struct DialogTimestamp(
    DateTimeOffset Timestamp,
    Guid DialogId,
    int DialogCounter)
{
    public static IEnumerable<DialogTimestamp> Generate(DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
    {
        var interval = TimeSpan.FromTicks((toDate.Ticks - fromDate.Ticks) / dialogAmount);
        return Enumerable.Range(0, dialogAmount)
            .Select(x =>
            {
                var timestamp = fromDate + (interval * x);
                var dialogId = DeterministicUuidV7.Create<Dialog>(timestamp, Guid.Empty);
                return new DialogTimestamp(timestamp, dialogId, x + 1);
            });
    }
}
