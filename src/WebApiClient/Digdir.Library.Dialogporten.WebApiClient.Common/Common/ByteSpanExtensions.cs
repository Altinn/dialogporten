using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace Altinn.ApiClients.Dialogporten.Common;

internal static class ByteSpanExtensions
{
    public static bool IsValidJson(this ReadOnlySpan<byte> span)
    {
        var reader = new Utf8JsonReader(span);
        try
        {
            while (reader.Read()) { }

            return true;
        }
        catch (JsonException) { }
        return false;
    }

    public static bool TryGetClaimsPrincipal(this ReadOnlySpan<byte> body,
        [NotNullWhen(true)] out ClaimsPrincipal? claimsPrincipal)
    {
        claimsPrincipal = null;
        try
        {
            claimsPrincipal = body.GetClaimsPrincipal();
            return true;
        }
        catch (JsonException) { /* swallow exception by design */ }

        return false;
    }

    [SuppressMessage("Style", "IDE0072:Add missing cases")]
    private static ClaimsPrincipal GetClaimsPrincipal(this ReadOnlySpan<byte> body)
    {
        var reader = new Utf8JsonReader(body);
        var claims = new List<Claim>();
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            var propertyName = reader.GetString()!;
            reader.Read();

            var claimValue = reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.TryGetInt64(out var longValue)
                    ? longValue.ToString(CultureInfo.InvariantCulture)
                    : reader.GetDouble().ToString(CultureInfo.InvariantCulture),
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                JsonTokenType.StartArray => ParseObject(ref reader),
                JsonTokenType.StartObject => ParseObject(ref reader),
                _ => null
            };
            if (claimValue is null) continue;
            claims.Add(new Claim(propertyName, claimValue));
        }

        var identity = new ClaimsIdentity(claims, "DialogToken");
        return new ClaimsPrincipal(identity);
    }

    private static string ParseObject(ref Utf8JsonReader reader)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        return jsonDoc.RootElement.GetRawText();
    }
}
