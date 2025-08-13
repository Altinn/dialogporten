namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

internal static class DialogTimestampExtensions
{
    public static Random GetRng(this DialogTimestamp dto) =>
        new(dto.DialogId.ToString().GetHashCode());

    public static string GetParty(this DialogTimestamp dto)
    {
        var rng = dto.GetRng();
        var partyIndex = rng.Next(0, Parties.List.Length);
        return Parties.List[partyIndex];
    }
}
