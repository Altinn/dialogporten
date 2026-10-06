using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

namespace Digdir.Domain.Dialogporten.Domain.Parties;

public sealed class NorwegianPersonIdentifier : IPartyIdentifier
{
    private static readonly int[] SocialSecurityNumberWeights1 = [3, 7, 6, 1, 8, 9, 4, 5, 2, 1];
    private static readonly int[] SocialSecurityNumberWeights2 = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2, 1];

    public static char ShortPrefix => 'p';
    private static string PersonPrefix => "urn:altinn:person:identifier-";
    public static string Prefix => PersonPrefix + "no";
    public static string PrefixWithSeparator => Prefix + PartyIdentifier.Separator;
    public static string HashPrefix => PersonPrefix + "ephemeral";
    public static string HashPrefixWithSeparator => HashPrefix + PartyIdentifier.Separator;
    public string FullId { get; }
    public string Id { get; }

    private NorwegianPersonIdentifier(ReadOnlySpan<char> value)
    {
        Id = value.ToString();
        FullId = PrefixWithSeparator + Id;
    }

    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPartyIdentifier? identifier)
    {
        identifier = IsValid(value)
            ? new NorwegianPersonIdentifier(PartyIdentifier.GetIdPart(value))
            : null;
        return identifier is not null;
    }

    public static bool IsValid(ReadOnlySpan<char> value)
    {
        var idNumberWithoutPrefix = PartyIdentifier.GetIdPart(value);
        return idNumberWithoutPrefix.Length == 11
               && Mod11.TryCalculateControlDigit(idNumberWithoutPrefix[..9], SocialSecurityNumberWeights1, out var control1)
               && Mod11.TryCalculateControlDigit(idNumberWithoutPrefix[..10], SocialSecurityNumberWeights2, out var control2)
               && control1 == int.Parse(idNumberWithoutPrefix[9..10], CultureInfo.InvariantCulture)
               && control2 == int.Parse(idNumberWithoutPrefix[10..11], CultureInfo.InvariantCulture);
    }
}
