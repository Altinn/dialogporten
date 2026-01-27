using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;

public static class PartyIdentifier
{
    private delegate bool TryParseDelegate(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPartyIdentifier? identifier);
    private static readonly Dictionary<string, TryParseDelegate> TryParseByPrefix = CreateTryParseByPrefix();
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

    internal static ReadOnlySpan<char> GetIdPart(ReadOnlySpan<char> value)
    {
        var separatorIndex = value.LastIndexOf(Separator);
        return separatorIndex == -1
            ? value
            : value[(separatorIndex + Separator.Length)..];
    }

    private static Dictionary<string, TryParseDelegate> CreateTryParseByPrefix()
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
            .ToDictionary(x => x.Prefix, x => x.TryParse);
    }

    extension(List<PartyIdentifierMetadata> partyIdentifiers)
    {
        private List<PartyIdentifierMetadata> AssertNoIdenticalPrefixes()
        {
            var identicalPrefix = partyIdentifiers
                .GroupBy(x => x.Prefix)
                .Where(x => x.Count() > 1)
                .ToList();

            if (identicalPrefix.Count != 0)
            {
                var typeNameGroups = string.Join(", ", identicalPrefix.Select(x => $"{{{string.Join(", ", x.Select(x => x.Type.Name))}}}"));
                throw new InvalidOperationException(
                    $"{nameof(IPartyIdentifier.Prefix)} cannot be identical to another {nameof(IPartyIdentifier)} for the following type groups: [{typeNameGroups}].");
            }

            return partyIdentifiers;
        }

        private List<PartyIdentifierMetadata> AssertPrefixEndsWithSeparator()
        {
            var separatorlessPrefix = partyIdentifiers
                .Where(x => !x.Prefix.EndsWith(Separator, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (separatorlessPrefix.Count != 0)
            {
                var typeNames = string.Join(", ", separatorlessPrefix.Select(x => x.Type.Name));
                throw new InvalidOperationException(
                    $"{nameof(IPartyIdentifier.Prefix)} must end with prefix-id separator '{Separator}' for the following types: [{typeNames}].");
            }

            return partyIdentifiers;
        }

        private List<PartyIdentifierMetadata> AssertPrefixNotNullOrWhitespace()
        {
            var nullOrWhitespacePrefix = partyIdentifiers
                .Where(x => string.IsNullOrWhiteSpace(x.Prefix))
                .ToList();

            if (nullOrWhitespacePrefix.Count != 0)
            {
                var typeNames = string.Join(", ", nullOrWhitespacePrefix.Select(x => x.Type.Name));
                throw new InvalidOperationException(
                    $"{nameof(IPartyIdentifier.Prefix)} cannot be null or whitespace for the following types: [{typeNames}]");
            }

            return partyIdentifiers;
        }
    }

    private record struct PartyIdentifierMetadata(
        Type Type,
        string Prefix,
        TryParseDelegate TryParse);
}
