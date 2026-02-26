using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

public static class PartyIdentifier
{
    private delegate bool TryParseDelegate(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPartyIdentifier? identifier);
    private static readonly List<PartyIdentifierMetadata> PartyIdentifiersMetadata = CreatePartyIdentifiersMetadata();
    private static readonly Dictionary<string, TryParseDelegate> TryParseByPrefix = PartyIdentifiersMetadata.ToDictionary(x => x.Prefix, x => x.TryParse);
    private static readonly Dictionary<string, char> ShortPrefixByPrefix = PartyIdentifiersMetadata.ToDictionary(x => x.Prefix, x => x.ShortPrefix);
    private static readonly Dictionary<char, string> PrefixByShortPrefix = PartyIdentifiersMetadata.ToDictionary(x => x.ShortPrefix, x => x.Prefix);
    public const string Separator = ":";

    public static string Prefix(this IPartyIdentifier identifier)
        => identifier.FullId[..(identifier.FullId.IndexOf(identifier.Id, StringComparison.Ordinal) - Separator.Length)];

    public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPartyIdentifier? identifier)
    {
        identifier = null;
        var separatorIndex = value.LastIndexOf(Separator);
        if (separatorIndex == -1)
        {
            return false;
        }

        var prefix = value[..(separatorIndex + Separator.Length)].ToString();
        return TryParseByPrefix.TryGetValue(prefix, out var tryParse)
            && tryParse(value, out identifier);
    }

    public static bool TryGetShortPrefix(IPartyIdentifier identifier, out char shortPrefix)
        => ShortPrefixByPrefix.TryGetValue(identifier.Prefix() + Separator, out shortPrefix);

    public static bool TryGetPrefixWithSeparator(char shortPrefix, [NotNullWhen(true)] out string? prefixWithSeparator)
        => PrefixByShortPrefix.TryGetValue(char.ToLowerInvariant(shortPrefix), out prefixWithSeparator);

    internal static ReadOnlySpan<char> GetIdPart(ReadOnlySpan<char> value)
    {
        var separatorIndex = value.LastIndexOf(Separator);
        return separatorIndex == -1
            ? value
            : value[(separatorIndex + Separator.Length)..];
    }

    private static List<PartyIdentifierMetadata> CreatePartyIdentifiersMetadata()
    {
        return typeof(IPartyIdentifier)
            .Assembly
            .GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(IPartyIdentifier)))
            .Select(partyIdentifierType => new PartyIdentifierMetadata
            (
                Type: partyIdentifierType,
                Prefix: (string)partyIdentifierType
                    .GetProperty(nameof(IPartyIdentifier.PrefixWithSeparator),
                        BindingFlags.Static | BindingFlags.Public)!
                    .GetValue(null)!,
                ShortPrefix: (char)partyIdentifierType
                    .GetProperty(nameof(IPartyIdentifier.ShortPrefix),
                        BindingFlags.Static | BindingFlags.Public)!
                    .GetValue(null)!,
                TryParse: partyIdentifierType
                    .GetMethod(nameof(IPartyIdentifier.TryParse), [
                        typeof(ReadOnlySpan<char>), typeof(IPartyIdentifier).MakeByRefType()
                    ])!
                    .CreateDelegate<TryParseDelegate>()
            ))
            .ToList()
            .AssertPrefixNotNullOrWhitespace()
            .AssertPrefixEndsWithSeparator()
            .AssertNoIdenticalPrefixes()
            .AssertShortPrefixNotDefault()
            .AssertNoIdenticalShortPrefixes();
    }

    extension(List<PartyIdentifierMetadata> partyIdentifiers)
    {
        private List<PartyIdentifierMetadata> AssertNoIdenticalPrefixes()
        {
            var identicalPrefix = partyIdentifiers
                .GroupBy(x => x.Prefix)
                .Where(x => x.Count() > 1)
                .ToList();

            if (identicalPrefix.Count == 0) return partyIdentifiers;
            var typeNameGroups = string.Join(", ", identicalPrefix.Select(x => $"{{{string.Join(", ", x.Select(x => x.Type.Name))}}}"));
            throw new InvalidOperationException(
                $"{nameof(IPartyIdentifier.Prefix)} cannot be identical to another {nameof(IPartyIdentifier)} for the following type groups: [{typeNameGroups}].");

        }

        private List<PartyIdentifierMetadata> AssertNoIdenticalShortPrefixes()
        {
            var identicalShortPrefixes = partyIdentifiers
                .GroupBy(x => x.ShortPrefix)
                .Where(x => x.Count() > 1)
                .ToList();

            if (identicalShortPrefixes.Count == 0) return partyIdentifiers;
            var typeNameGroups = string.Join(", ", identicalShortPrefixes.Select(x => $"{{{string.Join(", ", x.Select(y => y.Type.Name))}}}"));
            throw new InvalidOperationException(
                $"{nameof(IPartyIdentifier.ShortPrefix)} cannot be identical to another {nameof(IPartyIdentifier)} for the following type groups: [{typeNameGroups}].");

        }

        private List<PartyIdentifierMetadata> AssertPrefixEndsWithSeparator()
        {
            var separatorlessPrefix = partyIdentifiers
                .Where(x => !x.Prefix.EndsWith(Separator, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (separatorlessPrefix.Count == 0) return partyIdentifiers;
            var typeNames = string.Join(", ", separatorlessPrefix.Select(x => x.Type.Name));
            throw new InvalidOperationException(
                $"{nameof(IPartyIdentifier.Prefix)} must end with prefix-id separator '{Separator}' for the following types: [{typeNames}].");

        }

        private List<PartyIdentifierMetadata> AssertPrefixNotNullOrWhitespace()
        {
            var nullOrWhitespacePrefix = partyIdentifiers
                .Where(x => string.IsNullOrWhiteSpace(x.Prefix))
                .ToList();

            if (nullOrWhitespacePrefix.Count == 0) return partyIdentifiers;
            var typeNames = string.Join(", ", nullOrWhitespacePrefix.Select(x => x.Type.Name));
            throw new InvalidOperationException(
                $"{nameof(IPartyIdentifier.Prefix)} cannot be null or whitespace for the following types: [{typeNames}]");

        }

        private List<PartyIdentifierMetadata> AssertShortPrefixNotDefault()
        {
            var defaultShortPrefixes = partyIdentifiers
                .Where(x => x.ShortPrefix == default)
                .ToList();

            if (defaultShortPrefixes.Count == 0) return partyIdentifiers;
            var typeNames = string.Join(", ", defaultShortPrefixes.Select(x => x.Type.Name));
            throw new InvalidOperationException(
                $"{nameof(IPartyIdentifier.ShortPrefix)} cannot be default for the following types: [{typeNames}]");

        }
    }

    private record struct PartyIdentifierMetadata(
        Type Type,
        string Prefix,
        char ShortPrefix,
        TryParseDelegate TryParse);
}
