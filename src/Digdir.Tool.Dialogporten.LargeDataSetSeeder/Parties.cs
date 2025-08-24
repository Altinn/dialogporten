namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

internal static class Parties
{
    internal static readonly string[] List = File.ReadAllLines("./parties").Distinct().ToArray();
}
