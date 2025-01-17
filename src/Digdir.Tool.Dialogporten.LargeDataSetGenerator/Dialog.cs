namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator;

internal static class Dialog
{
    private static readonly string[] ServiceResources = File.ReadAllLines("./service_resources");
    private static readonly string[] Parties = File.ReadAllLines("./parties");

    public const string CopyCommand = """COPY "Dialog" ("Id", "CreatedAt", "Deleted", "DeletedAt", "DueAt", "ExpiresAt", "ExtendedStatus", "ExternalReference", "Org", "Party", "PrecedingProcess", "Process", "Progress", "Revision", "ServiceResource", "ServiceResourceType", "StatusId", "VisibleFrom", "UpdatedAt") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var serviceResourceIndex = dto.Counter % ServiceResources.Length;
        var serviceResource = ServiceResources[serviceResourceIndex];

        var partyIndex = dto.Counter % Parties.Length;
        var party = Parties[partyIndex];

        return
            $"{dto.DialogId},{dto.FormattedTimestamp},FALSE,,,,sql-generated,,ttd,{party},,,11,{Guid.NewGuid()},{serviceResource},GenericAccessResource,1,,{dto.FormattedTimestamp}";
    }
}
