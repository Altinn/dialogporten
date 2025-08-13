namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

internal static class DialogTimestampExtensions
{
    public static Random GetRng(this DialogTimestamp dto) =>
        new(dto.DialogId.ToString().GetHashCode());

    public static string GetParty(this Random random) =>
        Parties.List[random.Next(0, Parties.List.Length)];
}
