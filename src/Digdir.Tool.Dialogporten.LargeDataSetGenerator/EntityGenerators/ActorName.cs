using System.Collections.Concurrent;
using System.Text;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetGenerator.EntityGenerators;

internal static class ActorName
{
    public static async Task FetchInsertedActorNames()
    {
        await using var conn = NpgsqlDataSource.Create(Environment.GetEnvironmentVariable("CONN_STRING")!);
        var connection = await conn.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand("SELECT * FROM \"ActorName\"", connection);
        await using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var id = reader.GetGuid(0);
            var actorId = reader.GetString(1);
            InsertedActorNames.TryAdd(actorId, id);
        }
    }

    internal static readonly ConcurrentDictionary<string, Guid> InsertedActorNames = [];

    public const string CopyCommand =
        """COPY "ActorName" ("Id", "ActorId", "Name", "CreatedAt") FROM STDIN (FORMAT csv, HEADER false, NULL '')""";

    public static string Generate(DialogTimestamp dto)
    {
        var actorNameCsvData = new StringBuilder();

        var rng = new Random(dto.DialogId.GetHashCode());

        var dialogPartyIndex = rng.Next(0, Parties.List.Length);
        var dialogParty = Parties.List[dialogPartyIndex];

        var transmissionPartyIndex = rng.Next(0, Parties.List.Length);
        var transmissionParty = Parties.List[transmissionPartyIndex];

        var dialogPartyActorNameId = DeterministicUuidV7.Generate(dto.Timestamp, nameof(ActorName), 1);

        // Actor.cs Activity2, DialogSeenLog
        if (InsertedActorNames.TryAdd(dialogParty, dialogPartyActorNameId))
        {
            // ActorId(party) should result in the same names across dialogs
            var dialogPartyActorName =
                $"{PersonNames.List[rng.Next(0, PersonNames.List.Length)]} " +
                $"{PersonNames.List[rng.Next(0, PersonNames.List.Length)]}";

            actorNameCsvData.AppendLine(
                $"{dialogPartyActorNameId},{dialogParty},{dialogPartyActorName},{dto.FormattedTimestamp}");
        }

        var transmissionActorNameId = DeterministicUuidV7.Generate(dto.Timestamp, nameof(ActorName), 2);
        // Actor.cs Transmission1
        if (InsertedActorNames.TryAdd(transmissionParty, transmissionActorNameId))
        {
            var transmissionActorName =
                $"{PersonNames.List[rng.Next(0, PersonNames.List.Length)]} " +
                $"{PersonNames.List[rng.Next(0, PersonNames.List.Length)]}";

            actorNameCsvData.AppendLine(
                $"{transmissionActorNameId},{transmissionParty},{transmissionActorName},{dto.FormattedTimestamp}");
        }

        return actorNameCsvData.ToString();
    }

    internal static Guid GetActorNameId(string party) => InsertedActorNames[party];
}
