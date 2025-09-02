using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

internal static class StaticStore
{
    public static int? DialogAmount { get; set; }
    public static Resource[]? PrivResources { get; set; }
    public static Resource[]? DaglResources { get; set; }

    public static Resource GetRandomResource(string party, Random? rng = null)
    {
        rng ??= Random.Shared;

        if (party.StartsWith(NorwegianPersonIdentifier.PrefixWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            return PrivResources?[rng.Next(0, PrivResources.Length)] ?? throw new InvalidOperationException("PrivResources is not initialized");
        }

        if (party.StartsWith(NorwegianOrganizationIdentifier.PrefixWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            return DaglResources?[rng.Next(0, DaglResources.Length)] ?? throw new InvalidOperationException("DaglResources is not initialized");
        }

        throw new ArgumentException($"Party must be a valid Norwegian identifier. Got {party}.", nameof(party));
    }
}
