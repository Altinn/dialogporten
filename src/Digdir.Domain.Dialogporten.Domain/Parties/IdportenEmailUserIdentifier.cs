using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

namespace Digdir.Domain.Dialogporten.Domain.Parties;

public sealed record IdportenEmailUserIdentifier : IPartyIdentifier
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
        var email = PartyIdentifier.GetIdPart(value);
        return !email.IsEmpty;
    }
}
