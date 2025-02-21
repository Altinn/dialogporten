using System.Buffers;
using System.Buffers.Text;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Claims;
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

        if (!TryDecodeToken(token, tokenDecodeBuffer, out var tokenParts, out var decodedTokenParts))
        {
            validationResult.AddError(tokenPropertyName, "Invalid token format");
            return validationResult;
        }

        if (!VerifySignature(tokenParts, decodedTokenParts))
        {
            validationResult.AddError(tokenPropertyName, "Invalid signature");
        }

        if (!VerifyExpiration(decodedTokenParts))
        {
            validationResult.AddError(tokenPropertyName, "Token has expired");
        }
        if (!validationResult.IsValid)
        {
            return validationResult;
        }

        var body = decodedTokenParts.Body;
        validationResult.Claims = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>
                {
                    new(DialogTokenClaimTypes.C, body.GetProperty(DialogTokenClaimTypes.C).GetString() ?? string.Empty),
                    new(DialogTokenClaimTypes.Level, body.GetProperty(DialogTokenClaimTypes.Level).GetInt64().ToString(CultureInfo.InvariantCulture)),
                    new(DialogTokenClaimTypes.Party, body.GetProperty(DialogTokenClaimTypes.Party).GetString() ?? string.Empty),
                    new(DialogTokenClaimTypes.Scope, body.GetProperty(DialogTokenClaimTypes.Scope).GetString() ?? string.Empty),
                    new(DialogTokenClaimTypes.DialogId, body.GetProperty(DialogTokenClaimTypes.DialogId).GetString() ?? string.Empty),
                    new(DialogTokenClaimTypes.A, body.GetProperty(DialogTokenClaimTypes.A).GetString() ?? string.Empty),
                    new(DialogTokenClaimTypes.Issuer, body.GetProperty(DialogTokenClaimTypes.Issuer).GetString() ?? string.Empty),
                    new(DialogTokenClaimTypes.Expire, body.GetProperty(DialogTokenClaimTypes.Expire).GetInt64().ToString(CultureInfo.InvariantCulture)),
                    new(DialogTokenClaimTypes.IssuedAt, body.GetProperty(DialogTokenClaimTypes.IssuedAt).GetInt64().ToString(CultureInfo.InvariantCulture)),
                    new(DialogTokenClaimTypes.NotVisibleBefore, body.GetProperty(DialogTokenClaimTypes.NotVisibleBefore).GetInt64().ToString(CultureInfo.InvariantCulture))
                }, "DialogToken"));
        return validationResult;
    }

    private static bool TryDecodeToken(
        ReadOnlySpan<char> token,
        Span<byte> tokenDecodeBuffer,
        out JwksTokenParts<char> tokenParts,
        out JsonTokenParts decodedTokenParts
    )
    {
        decodedTokenParts = default;
        if (!TryGetTokenParts(token, out tokenParts) ||
            !TryDecodeParts(tokenDecodeBuffer, tokenParts, out var decodedToken))
        {
            return false;
        }

        // Validate that the header and body are valid JSON

        if (!TryParseJson(decodedToken.Header, out var headerJson) ||
            !TryParseJson(decodedToken.Body, out var bodyJson))
        {
            return false;
        }
        decodedTokenParts = new JsonTokenParts(headerJson, bodyJson, decodedToken.Signature);
        return true;
    }

    private static bool TryParseJson(ReadOnlySpan<byte> span, out JsonElement jsonElement)
    {
        jsonElement = default;
        var reader = new Utf8JsonReader(span);
        try
        {
            jsonElement = JsonElement.ParseValue(ref reader);
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
        JsonTokenParts decodedTokenParts)
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

    private bool VerifyExpiration(JsonTokenParts decodedTokenParts)
    {
        const string expiresPropertyName = "exp";
        if (!decodedTokenParts.Body.TryGetProperty(expiresPropertyName, out var expiresElement))
        {
            return false;
        }

        if (!expiresElement.TryGetInt64(out var expiresUnixTimeSeconds))
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

    private static bool TryGetPublicKey(ReadOnlyCollection<PublicKeyPair> keyPairs, JsonElement header, [NotNullWhen(true)] out PublicKey? publicKey)
    {
        const string kidPropertyName = "kid";
        publicKey = null;
        if (!header.TryGetProperty(kidPropertyName, out var tokenKid))
        {
            return false;
        }

        foreach (var (kid, key) in keyPairs)
        {
            if (!kid.AsSpan().SequenceEqual(tokenKid.GetString())) continue;
            publicKey = key;
            return true;
        }

        return false;
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

    private readonly ref struct JsonTokenParts
    {
        public JsonElement Header { get; }

        public JsonElement Body { get; }

        public ReadOnlySpan<byte> Signature { get; }

        public JsonTokenParts(JsonElement header, JsonElement body, ReadOnlySpan<byte> signature)
        {
            Header = header;
            Body = body;
            Signature = signature;
        }
    }
}

internal static class DialogTokenClaimTypes
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
