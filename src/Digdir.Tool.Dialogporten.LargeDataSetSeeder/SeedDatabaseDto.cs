using System.Threading.Channels;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

public record struct DialogTimestamp(
    DateTimeOffset Timestamp,
    string FormattedTimestamp,
    Guid DialogId,
    int DialogCounter);

internal sealed class SeedDatabaseDto
{
    public SeedDatabaseDto(DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
    {
        Interval = TimeSpan.FromTicks((toDate.Ticks - fromDate.Ticks) / dialogAmount);
        DialogAmount = dialogAmount;
        FromDate = fromDate;
        ToDate = toDate;
    }

    public IEnumerable<DialogTimestamp> GetDialogTimestamps(int divisor = 1, int remainder = 0)
    {
        divisor = Math.Max(1, divisor); // Ensure divisor is at least 1
        remainder = Math.Max(0, remainder % divisor); // Ensure remainder is valid

        return Enumerable.Range(0, DialogAmount)
            .Where(x => x % divisor == remainder)
            .Select(x =>
            {
                var timestamp = FromDate + (Interval * x);
                // TODO: Remove?
                var formattedTimestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss zzz");
                var dialogId = DeterministicUuidV7.Create(timestamp, nameof(DialogEntity));
                var counter = x + 1;
                return new DialogTimestamp(timestamp, formattedTimestamp, dialogId, counter);
            });
    }

    public DateTimeOffset FromDate { get; }
    public DateTimeOffset ToDate { get; }
    public TimeSpan Interval { get; }
    public int DialogAmount { get; }
}

// public sealed class JobBoard
// {
//     private readonly Channel<DialogTimestamp> _channel = Channel.CreateBounded<DialogTimestamp>(1000);
//
//     public void AddJobs(DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
//     {
//         var interval = TimeSpan.FromTicks((toDate.Ticks - fromDate.Ticks) / dialogAmount);
//         Enumerable.Range(0, dialogAmount)
//             .Select(x =>
//             {
//                 var timestamp = fromDate + (interval * x);
//                 var formattedTimestamp = timestamp.ToString("yyyy-MM-dd HH:mm:ss zzz");
//                 var dialogId = DeterministicUuidV7.Create(timestamp, nameof(DialogEntity));
//                 var counter = x + 1;
//                 return new DialogTimestamp(timestamp, formattedTimestamp, dialogId, counter);
//             });
//         _channel.Reader.
//         _channel.Writer.WriteAsync()
//     }
// }
