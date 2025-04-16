namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

internal static class PersonNames
{
    internal static readonly string[]
        List = File.Exists("./person_names")
            ? File.ReadAllLines("./person_names")
            : throw new FileNotFoundException("./person_names");
}
