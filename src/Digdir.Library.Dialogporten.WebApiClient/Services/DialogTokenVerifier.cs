using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using NSec.Cryptography;

namespace Digdir.Library.Dialogporten.WebApiClient.Services;

public sealed class DialogTokenVerifier(string kid, PublicKey publicKey)
{
    public bool Verify(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            return false;
        }
        var header = Base64Url.DecodeFromChars(parts[0]);

        var headerJson = JsonSerializer.Deserialize<JsonElement>(header);

        if (!headerJson.TryGetProperty("kid", out var value) && value.GetString() != kid)
        {
            return false;
        }

        var signature = Base64Url.DecodeFromChars(parts[2]);

        return SignatureAlgorithm.Ed25519.Verify(publicKey, Encoding.UTF8.GetBytes(parts[0] + '.' + parts[1]), signature);

    }
    public static Dictionary<string, object?> GetDialogTokenClaims(string token)
    {
        var claims = new Dictionary<string, object?>();

        var parts = token.Split('.');
        if (parts.Length != 3)
        {
            throw new ArgumentException("Invalid dialog token");
        }

        var bodyJson = JsonSerializer.Deserialize<JsonElement>(Base64Url.DecodeFromChars(parts[1]));

        // Maps bodyJson -> DialogTokenClaimTypes
        var fieldsInfo = typeof(DialogTokenClaimTypes).GetFields().Where(f => f.FieldType == typeof(string));
        foreach (var fieldInfo in fieldsInfo)
        {
            var value = fieldInfo.GetValue(null);
            if (value != null && bodyJson.TryGetProperty(value.ToString()!, out var jsonValue))
            {
                claims.Add(value.ToString()!, jsonValue);
            }
        }
        return claims;
    }
}

public static class DialogTokenClaimTypes
{
    public const string JwtId = "jti";
    public const string Issuer = "iss";
    public const string IssuedAt = "iat";
    public const string NotBefore = "nbf";
    public const string Expires = "exp";
    public const string AuthenticationLevel = "l";
    public const string AuthenticatedParty = "c";
    public const string DialogParty = "p";
    public const string SupplierParty = "u";
    public const string ServiceResource = "s";
    public const string DialogId = "i";
    public const string Actions = "a";
}
