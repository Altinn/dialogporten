using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record ActorName(Guid Id, string ActorId, string Name, DateTimeOffset CreatedAt)
{
    public static readonly ActorName[] Values = GenerateEntities().ToArray();

    public static Guid GetRandomId() =>
        Values[Random.Shared.Next(Values.Length)].Id;

    private static IEnumerable<ActorName> GenerateEntities()
    {
        // TODO: Kan vi fjerne Parties.List og kun ha Values i minnet?
        var personUrns = File.ReadLines("./parties")
            .Where(x => x.StartsWith(NorwegianPersonIdentifier.PrefixWithSeparator,
                StringComparison.OrdinalIgnoreCase));
        var personNames = File.ReadAllLines("./person_names");
        var actorNameIds = new HashSet<Guid>();

        foreach (var personUrn in personUrns)
        {
            var name = personNames[Random.Shared.Next(personNames.Length)] + " " +
                       personNames[Random.Shared.Next(personNames.Length)];
            var nextId = Guid.CreateVersion7();
            while (!actorNameIds.Add(nextId))
            {
                nextId = Guid.CreateVersion7();
            }
            yield return new ActorName(Id: nextId, ActorId: personUrn, Name: name, CreatedAt: DateTimeOffset.UtcNow);
        }
    }
}

