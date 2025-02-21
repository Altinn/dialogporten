using System.Buffers;
using System.Buffers.Text;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Common;
using NSec.Cryptography;

namespace Altinn.ApiClients.Dialogporten.Services;

internal sealed class DialogTokenValidator : IDialogTokenValidator
{
    private readonly IEdDsaSecurityKeysCache _publicKeysCache;
    private readonly IClock _clock;

    public DialogTokenValidator(IEdDsaSecurityKeysCache publicKeysCache, IClock clock)
    {
        _publicKeysCache = publicKeysCache;
        _clock = clock;
    }

    public IValidationResult Validate(ReadOnlySpan<char> token)
    {
        const string tokenPropertyName = "token";
        var validationResult = new DefaultValidationResult();
        Span<byte> tokenDecodeBuffer = stackalloc byte[Base64Url.GetMaxDecodedLength(token.Length)];

        if (!TryDecodeToken(token, tokenDecodeBuffer, out var tokenParts, out var decodedTokenParts, out var claims))
        {
            validationResult.AddError(tokenPropertyName, "Invalid token format");
            return validationResult;
        }
        validationResult.Claims = claims;

        if (!VerifySignature(tokenParts, decodedTokenParts))
        {
            validationResult.AddError(tokenPropertyName, "Invalid signature");
        }

        if (!VerifyExpiration(decodedTokenParts))
        {
            validationResult.AddError(tokenPropertyName, "Token has expired");
        }

        return validationResult;
    }

    private static bool TryDecodeToken(
        ReadOnlySpan<char> token,
        Span<byte> tokenDecodeBuffer,
        out JwksTokenParts<char> tokenParts,
        out JwksTokenParts<byte> decodedTokenParts,
        [NotNullWhen(true)] out DialogTokenClaims? claims)
    {
        claims = null;
        decodedTokenParts = default;
        if (!TryGetTokenParts(token, out tokenParts) ||
            !TryDecodeParts(tokenDecodeBuffer, tokenParts, out decodedTokenParts))
        {
            return false;
        }

        // Validate that the header and body are valid JSON
        return IsValidJson(decodedTokenParts.Header) &&
               IsValidJson(decodedTokenParts.Body);
    }

    private static bool IsValidJson(ReadOnlySpan<byte> span)
    {
        var reader = new Utf8JsonReader(span);
        var temp = new Dictionary<byte[], byte[]>();
        try
        {
            while (reader.Read())
            {
                var prop = reader.ValueSpan;
                reader.Read();
                var value = reader.ValueSpan;
                temp.Add(prop.ToArray(), value.ToArray());
            }

            return true;
        }
        catch (JsonException) { }
        return false;
    }

    private static bool TryGetTokenParts(ReadOnlySpan<char> token, out JwksTokenParts<char> tokenParts)
    {
        tokenParts = default;
        var enumerator = token.Split('.');
        // Header
        if (!enumerator.MoveNext()) return false;
        var header = token[enumerator.Current];

        // Body
        if (!enumerator.MoveNext()) return false;
        var body = token[enumerator.Current];

        // Signature
        if (!enumerator.MoveNext()) return false;
        var signature = token[enumerator.Current];

        tokenParts = new JwksTokenParts<char>(token, header, body, signature);
        return !enumerator.MoveNext();
    }

    private static bool TryDecodeParts(
        Span<byte> buffer,
        JwksTokenParts<char> parts,
        out JwksTokenParts<byte> decodedParts)
    {
        decodedParts = default;
        var bufferPointer = 0;
        if (!TryDecodePart(parts.Header, buffer, out var header, out var headerLength))
        {
            return false;
        }

        bufferPointer += headerLength;
        buffer[bufferPointer++] = (byte)'.';
        if (!TryDecodePart(parts.Body, buffer[bufferPointer..], out var body, out var bodyLength))
        {
            return false;
        }

        bufferPointer += bodyLength;
        buffer[bufferPointer++] = (byte)'.';
        if (!TryDecodePart(parts.Signature, buffer[bufferPointer..], out var signature, out _))
        {
            return false;
        }

        decodedParts = new JwksTokenParts<byte>(buffer, header, body, signature);
        return true;
    }

    private bool VerifySignature(
        JwksTokenParts<char> tokenParts,
        JwksTokenParts<byte> decodedTokenParts)
    {
        var publicKeys = _publicKeysCache.PublicKeys;
        if (publicKeys.Count == 0)
        {
            throw new InvalidOperationException(
                "No public keys available. Most likely due to an error when fetching the " +
                "public keys from the dialogporten well-known endpoint. Please check the " +
                "logs for more information. Alternatively, set " +
                "DialogportenSettings.ThrowOnPublicKeyFetchInit=true to ensure public " +
                "keys are fetched before starting up the application.");
        }

        var rawSignedPartLength = tokenParts.Header.Length + tokenParts.Body.Length + 1;
        Span<byte> signedPartBuffer = stackalloc byte[Encoding.UTF8.GetMaxByteCount(rawSignedPartLength)];
        if (!Encoding.UTF8.TryGetBytes(tokenParts.Buffer[..rawSignedPartLength], signedPartBuffer, out var signedPartLength))
        {
            return false;
        }

        var signedPart = signedPartBuffer[..signedPartLength];

        return TryGetPublicKey(publicKeys, decodedTokenParts.Header, out var publicKey)
               && SignatureAlgorithm.Ed25519.Verify(publicKey, signedPart, decodedTokenParts.Signature);
    }

    private bool VerifyExpiration(JwksTokenParts<byte> decodedTokenParts)
    {
        const string expiresPropertyName = "exp";
        if (!TryGetPropertyValue(decodedTokenParts.Body, expiresPropertyName, out var expiresSpan))
        {
            return false;
        }

        if (!Utf8Parser.TryParse(expiresSpan, out long expiresUnixTimeSeconds, out var bytesConsumed))
        {
            return false;
        }

        if (bytesConsumed != expiresSpan.Length)
        {
            return false;
        }

        var expires = DateTimeOffset.FromUnixTimeSeconds(expiresUnixTimeSeconds);
        return expires >= _clock.UtcNow;
    }

    private static bool TryDecodePart(ReadOnlySpan<char> tokenPart, Span<byte> buffer, out ReadOnlySpan<byte> span, out int length)
    {
        span = default;
        if (!TryDecodeFromChars(tokenPart, buffer, out length))
        {
            return false;
        }

        span = buffer[..length];
        return true;
    }

    private static bool TryDecodeFromChars(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
    {
        var result = Base64Url.DecodeFromChars(source, destination, out _, out bytesWritten);
        return result is OperationStatus.Done;
    }

    private static bool TryGetPublicKey(ReadOnlyCollection<PublicKeyPair> keyPairs, ReadOnlySpan<byte> header, [NotNullWhen(true)] out PublicKey? publicKey)
    {
        const string kidPropertyName = "kid";
        publicKey = null;
        if (!TryGetPropertyValue(header, kidPropertyName, out var tokenKid))
        {
            return false;
        }

        Span<char> kidCharBuffer = stackalloc char[Encoding.UTF8.GetMaxCharCount(tokenKid.Length)];
        if (!Encoding.UTF8.TryGetChars(tokenKid, kidCharBuffer, out var charsWritten))
        {
            return false;
        }

        foreach (var (kid, key) in keyPairs)
        {
            if (!kid.AsSpan().SequenceEqual(kidCharBuffer[..charsWritten])) continue;
            publicKey = key;
            return true;
        }

        return false;
    }

    private static bool TryGetPropertyValue(ReadOnlySpan<byte> json, ReadOnlySpan<char> name, out ReadOnlySpan<byte> value)
    {
        value = default;
        var reader = new Utf8JsonReader(json);
        while (reader.Read())
        {
            if (!IsPropertyName(reader, name)) continue;
            reader.Read();
            value = reader.ValueSpan;
            return true;
        }
        return false;
    }

    private static bool IsPropertyName(Utf8JsonReader reader, ReadOnlySpan<char> name)
    {
        return reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals(name);
    }

    private readonly ref struct JwksTokenParts<T>
        where T : unmanaged
    {
        public ReadOnlySpan<T> Buffer { get; }

        public ReadOnlySpan<T> Header { get; }

        public ReadOnlySpan<T> Body { get; }

        public ReadOnlySpan<T> Signature { get; }

        public JwksTokenParts(ReadOnlySpan<T> buffer, ReadOnlySpan<T> header, ReadOnlySpan<T> body, ReadOnlySpan<T> signature)
        {
            Buffer = buffer;
            Header = header;
            Body = body;
            Signature = signature;
        }
    }
}

internal sealed class DialogTokenClaimTypes
{
    public const string C = "c";
    public const string Level = "l";
    public const string Party = "p";
    public const string Scope = "s";
    public const string DialogId = "i";
    public const string A = "a";
    public const string Issuer = "iss";
    public const string IssuedAt = "iat";
    public const string NotVisibleBefore = "nbf";
    public const string Expire = "exp";
}

public sealed record DialogTokenClaims(
    string C,
    string Level,
    string Party,
    string Scope,
    string DialogId,
    string A,
    string Issuer,
    long IssuedAt,
    long NotVisibleBefore,
    long Expire
);
