using System.Collections.Concurrent;
using Npgsql;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CopyCommand;
using static Digdir.Tool.Dialogporten.LargeDataSetGenerator.CsvBuilder;

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

    public static readonly string CopyCommand = Create(nameof(ActorName), "Id", "ActorId", "Name", "CreatedAt");

    public static string Generate(DialogTimestamp dto) => BuildCsv(sb =>
    {
        var rng = dto.GetRng();

        var dialogParty = rng.GetParty();
        var transmissionParty = rng.GetParty();

        var dialogPartyActorNameId = DeterministicUuidV7.Generate(dto.Timestamp, nameof(ActorName), 1);

        if (InsertedActorNames.TryAdd(dialogParty, dialogPartyActorNameId))
        {
            var dialogPartyActorName =
                $"{PersonNames.List[rng.Next(0, PersonNames.List.Length)]} " +
                $"{PersonNames.List[rng.Next(0, PersonNames.List.Length)]}";

            sb.AppendLine($"{dialogPartyActorNameId},{dialogParty},{dialogPartyActorName},{dto.FormattedTimestamp}");
        }

        var transmissionActorNameId = DeterministicUuidV7.Generate(dto.Timestamp, nameof(ActorName), 2);
        if (InsertedActorNames.TryAdd(transmissionParty, transmissionActorNameId))
        {
            var transmissionActorName =
                $"{PersonNames.List[rng.Next(0, PersonNames.List.Length)]} " +
                $"{PersonNames.List[rng.Next(0, PersonNames.List.Length)]}";

            sb.AppendLine($"{transmissionActorNameId},{transmissionParty},{transmissionActorName},{dto.FormattedTimestamp}");
        }
    });

    internal static Guid GetActorNameId(string party) => InsertedActorNames[party];
}
