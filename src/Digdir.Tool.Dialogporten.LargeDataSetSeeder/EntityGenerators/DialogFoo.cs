using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

internal static class DialogFoo
{
    // private static readonly string[] ServiceResources = File.ReadAllLines("./service_resources");
    //
    // public static readonly string CopyCommand = CreateCopyCommand(nameof(Dialog),
    //     "Id", "CreatedAt", "Deleted", "DeletedAt", "DueAt", "ExpiresAt", "ExtendedStatus",
    //     "ExternalReference", "Org", "Party", "PrecedingProcess", "Process", "Progress",
    //     "Revision", "ServiceResource", "ServiceResourceType", "StatusId", "VisibleFrom", "UpdatedAt");

    public static string Generate(DialogTimestamp _)
    {
        return "foo";
        // var serviceResourceIndex = dto.DialogCounter % ServiceResources.Length;
        // var serviceResource = ServiceResources[serviceResourceIndex];
        //
        // // TODO: 1/X of dialogs should be from special party list.
        // var party = dto.GetRng().GetParty();
        //
        // var dialog = new Dialog()
        // {
        //     Id = dto.DialogId,
        //     CreatedAt = dto.Timestamp
        // };

        // return CsvBuilder.Magic(dialog);
        // return
        //     $"{dto.DialogId}," +
        //     $"{dto.FormattedTimestamp}," +
        //     $"FALSE," +
        //     $"{Null}," +
        //     $"{Null}," +
        //     $"{Null}," +
        //     $"sql-generated," +
        //     $"{Null}," +
        //     $"ttd,{party}," +
        //     $"{Null}," +
        //     $"{Null}," +
        //     $"11,{Guid.NewGuid()},{serviceResource},GenericAccessResource,1,{Null},{dto.FormattedTimestamp}";
    }
}

public sealed class Dialog
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
