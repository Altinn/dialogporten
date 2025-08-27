using System.Collections.Concurrent;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Npgsql;
using static Digdir.Tool.Dialogporten.LargeDataSetSeeder.Utils;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record ActorName(Guid Id, string ActorId, string Name, DateTimeOffset CreatedAt)
{
    private static readonly List<Guid> ActorNameIds = [];

    public static IEnumerable<ActorName> GenerateEntities()
    {
        var personUrns = Parties.List
            .Where(x => x.StartsWith(NorwegianPersonIdentifier.PrefixWithSeparator,
                StringComparison.OrdinalIgnoreCase));
        HashSet<Guid> actorNameIds = [];

        foreach (var personUrn in personUrns)
        {
            var name = PersonNames.List[Random.Shared.Next(PersonNames.List.Length)] + " " +
                       PersonNames.List[Random.Shared.Next(PersonNames.List.Length)];
            var nextId = Guid.CreateVersion7();
            while (!actorNameIds.Add(nextId))
            {
                nextId = Guid.CreateVersion7();
            }
            yield return new ActorName(Id: nextId, ActorId: personUrn, Name: name, CreatedAt: DateTimeOffset.UtcNow);
        }

        ActorNameIds.AddRange(actorNameIds);
    }

    public static Guid GetRandomId()
    {
        if (ActorNameIds.Count == 0)
        {
            throw new InvalidOperationException("ActorName is not seeded.");
        }

        return ActorNameIds[Random.Shared.Next(ActorNameIds.Count)];
    }
}

