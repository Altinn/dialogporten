using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

public readonly record struct DialogTimestamp(
    DateTimeOffset Timestamp,
    Guid DialogId,
    int DialogCounter)
{
    public Random GetRng() =>
        new(DialogId.ToString().GetHashCode());

    public Guid ToUuidV7<TEntity>(Guid parentId, int tieBreaker = 0) =>
        DeterministicUuidV7.Create<TEntity>(Timestamp, parentId, tieBreaker);

    public static IEnumerable<DialogTimestamp> Generate(DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
    {
        var interval = TimeSpan.FromTicks((toDate.Ticks - fromDate.Ticks) / dialogAmount);
        return Enumerable.Range(0, dialogAmount)
            .Select(x =>
            {
                var timestamp = fromDate + (interval * x);
                var dialogId = DeterministicUuidV7.Create<Dialog>(timestamp, Guid.Empty, x);
                return new DialogTimestamp(timestamp, dialogId, x + 1);
            });
    }
}
