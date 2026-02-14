using System.Diagnostics.CodeAnalysis;

namespace Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

public interface IPartyIdentifier
{
    string FullId { get; }
    string Id { get; }
    static abstract char ShortPrefix { get; }
    static abstract string Prefix { get; }
    static abstract string PrefixWithSeparator { get; }
    static abstract bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPartyIdentifier? identifier);
}
