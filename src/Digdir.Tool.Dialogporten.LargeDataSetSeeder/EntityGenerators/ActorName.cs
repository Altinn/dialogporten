using Bogus;
using Bogus.Extensions.Norway;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record ActorName(Guid Id, string ActorId, string Name, DateTimeOffset CreatedAt)
{
    public static readonly ActorName[] Values = GenerateEntities(100_000).ToArray();

    public static Guid GetRandomId() =>
        Values[Random.Shared.Next(Values.Length)].Id;

    private static IEnumerable<ActorName> GenerateEntities(int count)
    {
        var existing = new HashSet<(string, string)>();
        var faker = new Faker<ActorName>("nb_NO")
            .UseSeed(0123456789)
            .CustomInstantiator(x => new ActorName(
                Guid.CreateVersion7(),
                $"{NorwegianPersonIdentifier.PrefixWithSeparator}{x.Person.Fodselsnummer()}",
                x.Person.FullName,
                DateTimeOffset.UtcNow));

        for (var i = 0; i < count; i++)
        {
            var actorName = faker.Generate();
            while (!existing.Add((actorName.Name, actorName.ActorId)))
            {
                actorName = faker.Generate();
            }
            yield return actorName;
        }
    }
}

