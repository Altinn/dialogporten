using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

namespace Digdir.Domain.Dialogporten.Domain.Parties;

public record NorwegianOrganizationIdentifier : IPartyIdentifier
{
    private static readonly int[] OrgNumberWeights = { 3, 2, 7, 6, 5, 4, 3, 2 };
    public static string Prefix { get; } = "urn:altinn:organization:identifier-no::";
    public string FullId { get; }
    public string Id { get; }

    private NorwegianOrganizationIdentifier(ReadOnlySpan<char> value)
    {
        Id = value.ToString();
        FullId = Prefix + Id;
    }

    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPartyIdentifier? identifier)
    {
        identifier = IsValid(value)
            ? new NorwegianOrganizationIdentifier(PartyIdentifier.GetIdPart(value))
            : null;
        return identifier is not null;
    }

    public static bool IsValid(ReadOnlySpan<char> value)
    {
        var idNumberWithoutPrefix = PartyIdentifier.GetIdPart(value);
        return idNumberWithoutPrefix.Length == 9
               && Mod11.TryCalculateControlDigit(idNumberWithoutPrefix[..8], OrgNumberWeights, out var control)
               && control == int.Parse(idNumberWithoutPrefix[8..9], CultureInfo.InvariantCulture);
    }
}
