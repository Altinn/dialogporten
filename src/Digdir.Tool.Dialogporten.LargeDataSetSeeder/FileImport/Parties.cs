namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.FileImport;

internal static class Parties
{
    internal static readonly string[] List = File.ReadAllLines("./parties").Distinct().ToArray();
}
