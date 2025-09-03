using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

internal static class KnownParties
{
    internal static readonly KnownParty[] Values;

    static KnownParties()
    {
        var knownSsns = File
            .ReadLines(Path.Combine(AppContext.BaseDirectory, "endusers-yt01.csv"))
            .Skip(1)
            .Select(line => line.Split(',')[0])
            .ToHashSet();

        Values = File
            .ReadLines(Path.Combine(AppContext.BaseDirectory, "./OrgsInYt01.csv"))
            .Skip(1)
            .Select(x =>
            {
                var split = x.Split(';');
                return (OrgNr: split[0], Ssn: split[2]);
            })
            .GroupBy(x => x.Ssn)
            .IntersectBy(knownSsns, x => x.Key)
            .Select(x => new KnownParty(x.Key, x.Select(t => t.OrgNr).ToList()))
            .OrderByDescending(x => x.Orgs.Count)
            .ToArray();
    }
}

internal sealed record KnownParty(string Ssn, List<string> Orgs)
{
    public string GetRandomPartyUrn(Random? rng = null)
    {
        rng ??= Random.Shared;
        var index = rng.Next(0, Orgs.Count + 1);
        return index < Orgs.Count
            ? $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}{Orgs[index]}"
            : $"{NorwegianPersonIdentifier.PrefixWithSeparator}{Ssn}";
    }
}
