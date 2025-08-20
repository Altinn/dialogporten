using Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

internal static class DialogTimestampExtensions
{
    public static Random GetRng(this DialogTimestamp dto) =>
        new(dto.DialogId.ToString().GetHashCode());

    public static string GetParty(this Random random) =>
        Parties.List[random.Next(0, Parties.List.Length)];

    public static (Guid dialogPartyActorNameId, Guid transmissionPartyActorNameId) GetActorNameIds(this DialogTimestamp dto)
    {
        var rng = dto.GetRng();
        var dialogParty = rng.GetParty();
        var transmissionParty = rng.GetParty();

        var dialogPartyActorNameId = ActorName.GetActorNameId(dialogParty);
        var transmissionPartyActorNameId = ActorName.GetActorNameId(transmissionParty);

        return (dialogPartyActorNameId, transmissionPartyActorNameId);
    }
}
