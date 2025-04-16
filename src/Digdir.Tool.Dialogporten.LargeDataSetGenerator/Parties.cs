namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

internal static class Parties
{
    internal static readonly string[] List = File.ReadAllLines("./parties").Distinct().ToArray();
}
