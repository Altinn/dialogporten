
namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal static class DialogTimestampExtensions
{
    public static Random GetRng(this DialogTimestamp dto) =>
        new(dto.DialogId.ToString().GetHashCode());

    public static Guid ToUuidV7<TEntity>(this DialogTimestamp dto, Guid parentId, int tieBreaker = 0)
        => DeterministicUuidV7.Create<TEntity>(dto.Timestamp, parentId, tieBreaker);
}
