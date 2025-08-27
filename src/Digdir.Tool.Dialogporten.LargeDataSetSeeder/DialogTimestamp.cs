using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

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
                var dialogId = DeterministicUuidV7.Create(timestamp, nameof(DialogEntity));
                return new DialogTimestamp(timestamp, dialogId, x + 1);
            });
    }
}
