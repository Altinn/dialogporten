using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

namespace Digdir.Domain.Dialogporten.Domain.Parties;

public sealed record FeideUserIdentifier : IPartyIdentifier
{
    public static string Prefix => "urn:altinn:feide-subject";
    public static string PrefixWithSeparator => Prefix + PartyIdentifier.Separator;
    public string FullId { get; }
    public string Id { get; }

    private FeideUserIdentifier(ReadOnlySpan<char> value)
    {
        Id = value.ToString();
        FullId = PrefixWithSeparator + Id;
    }

    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPartyIdentifier? identifier)
    {
        identifier = IsValid(value)
            ? new FeideUserIdentifier(PartyIdentifier.GetIdPart(value))
            : null;
        return identifier is not null;
    }

    public static bool IsValid(ReadOnlySpan<char> value)
    {
        ReadOnlySpan<char> idPart;
        if (value.StartsWith(PrefixWithSeparator))
        {
            idPart = PartyIdentifier.GetIdPart(value);
        }
        else
        {
            return IsValid(string.Concat(PrefixWithSeparator, value).AsSpan());
        }

        return Uri.IsWellFormedUriString(value.ToString(), UriKind.Absolute)
               && Uri.UnescapeDataString(idPart.ToString()).Length == 64;
    }
}
