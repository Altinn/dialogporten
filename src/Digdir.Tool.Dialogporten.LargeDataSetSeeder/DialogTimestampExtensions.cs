using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

internal static class DialogTimestampExtensions
{
    public static Random GetRng(this DialogTimestamp dto) =>
        new(dto.DialogId.ToString().GetHashCode());

    public static string GetParty(this Random random) =>
        Parties.List[random.Next(0, Parties.List.Length)];

    public static (Guid dialogPartyActorNameId, Guid transmissionPartyActorNameId) GetActorNameIds(this DialogTimestamp _)
    {
        // var rng = dto.GetRng();
        // var dialogParty = rng.GetParty();
        // var transmissionParty = rng.GetParty();

        // var dialogPartyActorNameId = ActorName.GetActorNameId(dialogParty);
        // var transmissionPartyActorNameId = ActorName.GetActorNameId(transmissionParty);

        // return (dialogPartyActorNameId, transmissionPartyActorNameId);
        return (Guid.Empty, Guid.Empty);
    }

    public static Guid ToUuidV7(this DialogTimestamp dto, string tableName, int tieBreaker = 0)
        => DeterministicUuidV7.Create(dto.Timestamp, tableName, tieBreaker);
}
