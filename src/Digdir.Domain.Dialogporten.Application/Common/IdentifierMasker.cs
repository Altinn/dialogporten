
using System.Security.Cryptography;
using System.Text;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Application.Common;

public static class IdentifierMasker
{
    public const int StringLength = 10;
    private const int SaltSize = 16;
    private static readonly byte[] Salt = RandomNumberGenerator.GetBytes(SaltSize);

    private static string? Hash(string? plaintext)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            return null;
        }

        var identifierBytes = Encoding.UTF8.GetBytes(plaintext);
        Span<byte> buffer = stackalloc byte[identifierBytes.Length + Salt.Length];
        identifierBytes.CopyTo(buffer);
        Salt.CopyTo(buffer[identifierBytes.Length..]);

        var hashBytes = SHA256.HashData(buffer);

        return Convert.ToHexStringLower(hashBytes, 0, StringLength / 2);
    }

    public static string? GetMaybeMaskedIdentifier(string? identifier) =>
        // We only care about masking norwegian person identifiers
        identifier is null || !identifier.StartsWith(NorwegianPersonIdentifier.Prefix, StringComparison.Ordinal)
            ? identifier
            : NorwegianPersonIdentifier.HashPrefixWithSeparator + Hash(identifier);
}
