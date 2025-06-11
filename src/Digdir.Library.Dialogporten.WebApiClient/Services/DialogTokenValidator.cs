using System.Buffers;
using System.Buffers.Text;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
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

    public IValidationResult Validate(ReadOnlySpan<char> token,
        Guid? dialogId = null,
        string[]? requiredActions = null,
        DialogTokenValidationParameters? options = null)
    {
        const string tokenPropertyName = "token";
        options ??= DialogTokenValidationParameters.Default;
        var validationResult = new DefaultValidationResult();
        Span<byte> tokenDecodeBuffer = stackalloc byte[Base64Url.GetMaxDecodedLength(token.Length)];

        if (!TryDecodeToken(token, tokenDecodeBuffer, out var tokenParts, out var decodedTokenParts, out var claimsPrincipal))
        {
            validationResult.AddError(tokenPropertyName, "Invalid token format");
            return validationResult;
        }

        validationResult.ClaimsPrincipal = claimsPrincipal;
        if (!VerifySignature(tokenParts, decodedTokenParts))
        {
            validationResult.AddError(tokenPropertyName, "Invalid signature");
        }

        if (options.ValidateLifetime && !claimsPrincipal.VerifyNotValidBefore(_clock, options.ClockSkew))
        {
            validationResult.AddError(tokenPropertyName, "Invalid nbf");
        }

        if (options.ValidateLifetime && !claimsPrincipal.VerifyExpirationTime(_clock, options.ClockSkew))
        {
            validationResult.AddError(tokenPropertyName, "Invalid exp");
        }

        if (dialogId.HasValue && !validationResult.ClaimsPrincipal.VerifyDialogId(dialogId.Value))
        {
            validationResult.AddError(tokenPropertyName, "Invalid dialog ID");
        }

        if (requiredActions is not null && !validationResult.ClaimsPrincipal.VerifyActions(requiredActions))
        {
            validationResult.AddError(tokenPropertyName, "Invalid actions");
        }

        return validationResult;
    }

    private static bool TryDecodeToken(
        ReadOnlySpan<char> token,
        Span<byte> tokenDecodeBuffer,
        out JwksTokenParts<char> tokenParts,
        out JwksTokenParts<byte> decodedTokenParts,
        [NotNullWhen(true)] out ClaimsPrincipal? claimsPrincipal)
    {
        decodedTokenParts = default;
        claimsPrincipal = null;
        if (!TryGetTokenParts(token, out tokenParts) ||
            !TryDecodeParts(tokenDecodeBuffer, tokenParts, out decodedTokenParts))
        {
            return false;
        }

        // Validate that the header and body are valid JSON
        return decodedTokenParts.Header.IsValidJson() &&
            decodedTokenParts.Body.TryGetClaimsPrincipal(out claimsPrincipal);
    }
#if NET8_0

    private static bool TryGetTokenParts(ReadOnlySpan<char> token, out JwksTokenParts<char> tokenParts)
    {
        tokenParts = default;
        var start = 0;
        var end = token.IndexOf('.');
        if (end == -1) return false;
        var header = token[start..end];

        start = end + 1;
        end = token[start..].IndexOf('.');
        if (end == -1) return false;
        var body = token[start..(start + end)];

        start = start + end + 1;
        end = token.Length;
        if (end == -1) return false;
        var signature = token[start..end];

        tokenParts = new JwksTokenParts<char>(token, header, body, signature);
        return true;
    }
#else
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
#endif

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
#if NET8_0

        try
        {

            var decoded = Base64Url.DecodeFromChars(source.ToString());
            bytesWritten = decoded.Length;
            decoded.CopyTo(destination);
            return true;
        }
        catch (FormatException)
        {
            bytesWritten = 0;
            return false;
        }
#else
        var result = Base64Url.DecodeFromChars(source, destination, out _, out bytesWritten);
        return result is OperationStatus.Done;
#endif
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
#if NET8_0
public static class Base64Url
{
    public static int GetMaxDecodedLength(int length) => (length + 2) / 3 * 4;

    public static void Encode(ReadOnlySpan<byte> data, Span<byte> destination, out int written)
    {
        Base64.EncodeToUtf8(data, destination, out _, out written);
        for (var i = 0; i < written; i++)
        {
            destination[i] = destination[i] switch
            {
                (byte)'+' => (byte)'-',
                (byte)'/' => (byte)'_',
                _ => destination[i]
            };
        }
        while (written > 0 && destination[written - 1] == '=') written--;
    }

    public static byte[] DecodeFromChars(string input)
    {
        var output = input;
        output = output.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 0: break;
            case 2: output += "=="; break;
            case 3: output += "="; break;
            default: throw new ArgumentException("Illegal base64url string", nameof(input));
        }

        return Convert.FromBase64String(output);
    }
}
#endif
