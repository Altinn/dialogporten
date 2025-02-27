namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Dialog
{
    private static readonly string[] ServiceResources = File.ReadAllLines("./service_resources");

    public const string CopyCommand = """COPY "Dialog" ("Id", "CreatedAt", "Deleted", "DeletedAt", "DueAt", "ExpiresAt", "ExtendedStatus", "ExternalReference", "Org", "Party", "PrecedingProcess", "Process", "Progress", "Revision", "ServiceResource", "ServiceResourceType", "StatusId", "VisibleFrom", "UpdatedAt") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var serviceResourceIndex = dto.DialogCounter % ServiceResources.Length;
        var serviceResource = ServiceResources[serviceResourceIndex];

        var rng = new Random(dto.DialogId.GetHashCode());
        var partyIndex = rng.Next(0, Parties.List.Length);
        var party = Parties.List[partyIndex];

        return
            $"{dto.DialogId},{dto.FormattedTimestamp},FALSE,,,,sql-generated,,ttd,{party},,,11,{Guid.NewGuid()},{serviceResource},GenericAccessResource,1,,{dto.FormattedTimestamp}";
    }
}
