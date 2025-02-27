namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class EndUserContext
{
    public const string CopyCommand = """COPY "DialogEndUserContext" ("Id", "CreatedAt", "UpdatedAt", "Revision", "DialogId", "SystemLabelId") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto) => $"{dto.DialogId},{dto.FormattedTimestamp},{dto.FormattedTimestamp},{Guid.NewGuid()},{dto.DialogId},1";
}
