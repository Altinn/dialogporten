using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators.CopyCommand;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class Dialog
{
    private static readonly string[] ServiceResources = File.ReadAllLines("./service_resources");

    public static readonly string CopyCommand = Create(nameof(Dialog),
        "Id", "CreatedAt", "Deleted", "DeletedAt", "DueAt", "ExpiresAt", "ExtendedStatus",
        "ExternalReference", "Org", "Party", "PrecedingProcess", "Process", "Progress",
        "Revision", "ServiceResource", "ServiceResourceType", "StatusId", "VisibleFrom", "UpdatedAt");

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
