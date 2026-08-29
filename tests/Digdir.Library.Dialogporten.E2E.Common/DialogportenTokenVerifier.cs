using System.Buffers.Text;
using System.Text.Json;
using NSec.Cryptography;

namespace Digdir.Library.Dialogporten.E2E.Common;

/// <summary>
/// Verifies dialog tokens and context tokens the way a receiving service must: fetch the published JWKS,
/// match the token's "kid", and check the Ed25519 signature over the signed part. Nothing here trusts the
/// token's own contents before the signature has been verified.
/// </summary>
public static class DialogportenTokenVerifier
{
    private const string JwksPath = "api/v1/.well-known/jwks.json";

    /// <summary>
    /// Verifies the token's signature against the JWKS published by the instance under test, and returns its
    /// decoded JOSE header and payload. Throws when the token is malformed, its key is unknown, or the
    /// signature does not verify.
    /// </summary>
    public static async Task<VerifiedToken> VerifyAsync(Uri webApiUri, string token, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(webApiUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            throw new InvalidOperationException($"Expected a compact JWS with three parts, got {parts.Length}.");
        }

        using var headerDocument = JsonDocument.Parse(Base64UrlDecode(parts[0]));
        using var payloadDocument = JsonDocument.Parse(Base64UrlDecode(parts[1]));

        var header = headerDocument.RootElement;
        var keyId = header.GetProperty("kid").GetString()
                    ?? throw new InvalidOperationException("Token header has no 'kid'.");
        var tokenType = header.TryGetProperty("typ", out var typ) ? typ.GetString() : null;

        var publicKey = await GetPublicKeyAsync(webApiUri, keyId, cancellationToken);

        // The signed part is the raw ASCII of "<header>.<payload>" — not the decoded bytes.
        var signedPart = System.Text.Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
        var signature = Base64UrlDecode(parts[2]);

        if (!SignatureAlgorithm.Ed25519.Verify(publicKey, signedPart, signature))
        {
            throw new InvalidOperationException(
                $"Token signature did not verify against JWKS key '{keyId}' from {webApiUri}.");
        }

        // The payload document is disposed with the using above, so hand back a detached clone.
        return new VerifiedToken(tokenType, keyId, payloadDocument.RootElement.Clone());
    }

    private static async Task<PublicKey> GetPublicKeyAsync(Uri webApiUri, string keyId, CancellationToken cancellationToken)
    {
        var jwksUri = BuildJwksUri(webApiUri);

        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(jwksUri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jwks = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(jwks);

        foreach (var key in document.RootElement.GetProperty("keys").EnumerateArray())
        {
            if (key.GetProperty("kid").GetString() != keyId)
            {
                continue;
            }

            var x = key.GetProperty("x").GetString()
                    ?? throw new InvalidOperationException($"JWKS key '{keyId}' has no 'x'.");

            return PublicKey.Import(SignatureAlgorithm.Ed25519, Base64UrlDecode(x), KeyBlobFormat.RawPublicKey);
        }

        throw new InvalidOperationException($"JWKS at {jwksUri} has no key with kid '{keyId}'.");
    }

    // A BaseAddress without a trailing slash treats its last path segment as replaceable when combined with a
    // relative request URI (RFC 3986 merge) - e.g. "https://host/dialogporten" + "api/v1/x" resolves to
    // "https://host/api/v1/x", silently dropping "/dialogporten". Remote environments configure the base URI
    // with exactly that shape, so build the full URI directly instead of relying on HttpClient.BaseAddress
    // combination (public for direct unit-test coverage of the URI arithmetic).
    public static Uri BuildJwksUri(Uri webApiUri) =>
        new(new Uri(webApiUri.AbsoluteUri.TrimEnd('/') + "/"), JwksPath);

    private static byte[] Base64UrlDecode(string value) => Base64Url.DecodeFromChars(value);
}

/// <summary>
/// A token whose signature has been verified against the published JWKS.
/// </summary>
public sealed class VerifiedToken
{
    internal VerifiedToken(string? tokenType, string keyId, JsonElement claims)
    {
        TokenType = tokenType;
        KeyId = keyId;
        Claims = claims;
    }

    /// <summary>The JOSE "typ" header, distinguishing the dialog token ("JWT") from "dialogcontexttoken+jwt".</summary>
    public string? TokenType { get; }

    public string KeyId { get; }

    public JsonElement Claims { get; }

    /// <summary>The claim as a string, or null when absent.</summary>
    public string? GetStringOrNull(string claim) =>
        Claims.TryGetProperty(claim, out var value) && value.ValueKind is not JsonValueKind.Null
            ? value.GetString()
            : null;

    /// <summary>The claim as a string, failing when absent.</summary>
    public string GetString(string claim) =>
        GetStringOrNull(claim)
        ?? throw new InvalidOperationException($"Token has no '{claim}' claim. Claims: {Claims.GetRawText()}");

    /// <summary>The claim as a list of strings, failing when absent. Accepts a single string as a one-item list.</summary>
    public List<string> GetStringList(string claim)
    {
        if (!Claims.TryGetProperty(claim, out var value))
        {
            throw new InvalidOperationException($"Token has no '{claim}' claim. Claims: {Claims.GetRawText()}");
        }

        if (value.ValueKind is JsonValueKind.Array)
        {
            return [.. value.EnumerateArray().Select(x => x.GetString()!)];
        }

        if (value.ValueKind is JsonValueKind.String)
        {
            return [value.GetString()!];
        }

        throw new InvalidOperationException($"Claim '{claim}' is {value.ValueKind}, expected a string or array.");
    }

    public bool HasClaim(string claim) => Claims.TryGetProperty(claim, out _);

    public override string ToString() => $"typ={TokenType} kid={KeyId} claims={Claims.GetRawText()}";
}
