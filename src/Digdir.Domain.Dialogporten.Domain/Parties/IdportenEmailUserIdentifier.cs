using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

namespace Digdir.Domain.Dialogporten.Domain.Parties;

public sealed partial record IdportenEmailUserIdentifier : IPartyIdentifier
{
    public static string Prefix => "urn:altinn:person:idporten-email";
    public static string PrefixWithSeparator => Prefix + PartyIdentifier.Separator;
    public string FullId { get; }
    public string Id { get; }

    private IdportenEmailUserIdentifier(ReadOnlySpan<char> value)
    {
        Id = value.ToString();
        FullId = PrefixWithSeparator + Id;
    }

    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPartyIdentifier? identifier)
    {
        identifier = IsValid(value)
            ? new IdportenEmailUserIdentifier(PartyIdentifier.GetIdPart(value))
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
               && EmailRegex().IsMatch(Uri.UnescapeDataString(idPart.ToString()));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
