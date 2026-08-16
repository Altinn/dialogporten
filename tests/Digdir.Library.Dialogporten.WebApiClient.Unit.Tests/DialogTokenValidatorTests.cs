using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.Common;
using Altinn.ApiClients.Dialogporten.Services;
using NSec.Cryptography;
using NSubstitute;
using Base64Url = System.Buffers.Text.Base64Url;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests;

public class DialogTokenValidatorTests
{
    private static readonly DateTimeOffset ValidTimeStamp = DateTimeOffset.Parse("2025-02-14T09:00:00Z", CultureInfo.InvariantCulture);
    private const string DialogToken =
        "eyJhbGciOiJFZERTQSIsInR5cCI6IkpXVCIsImtpZCI6ImRwLXN0YWdpbmctMjQwMzIyLW81eW1uIn0.eyJqdGkiOiIzOGNmZGNiOS0zODhiLTQ3YjgtYTFiZi05ZjE1YjI4MTk4OTQiLCJjIjoidXJuOmFsdGlubjpwZXJzb246aWRlbnRpZmllci1ubzoxNDg4NjQ5ODIyNiIsImwiOjMsInAiOiJ1cm46YWx0aW5uOnBlcnNvbjppZGVudGlmaWVyLW5vOjE0ODg2NDk4MjI2IiwicyI6InVybjphbHRpbm46cmVzb3VyY2U6ZGFnbC1jb3JyZXNwb25kZW5jZSIsImkiOiIwMTk0ZmU4Mi05MjgwLTc3YTUtYTdjZC01ZmYwZTZhNmZhMDciLCJhIjoicmVhZCIsImlzcyI6Imh0dHBzOi8vcGxhdGZvcm0udHQwMi5hbHRpbm4ubm8vZGlhbG9ncG9ydGVuL2FwaS92MSIsImlhdCI6MTczOTUyMzM2NywibmJmIjoxNzM5NTIzMzY3LCJleHAiOjE3Mzk1MjM5Njd9.O_f-RJhRPT7B76S7aOGw6jfxKDki3uJQLLC8nVlcNVJWFIOQUsy6gU4bG1ZdqoMBZPvb2K2X4I5fGpHW9dQMAA";
    private static readonly JsonSerializerOptions RelaxedHeaderOptions =
        new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly JsonSerializerOptions EscapingHeaderOptions =
        new() { Encoder = JavaScriptEncoder.Default };
    private static readonly string[] NonObjectHeader = ["not", "an", "object"];
    private static readonly PublicKeyPair[] ValidPublicKeyPairs =
    [
        new("dp-staging-240322-o5ymn", ToPublicKey("zs9hR9oqgf53th2lTdrBq3C1TZ9UlR-HVJOiUpWV63o")),
        new("dp-staging-240322-rju3g", ToPublicKey("23Sijekv5ATW4sSEiRPzL_rXH-zRV8MK8jcs5ExCmSU"))
    ];

    [Fact]
    public void ShouldReturnIsValid_GivenValidToken()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        // Act
        var result = sut.Validate(DialogToken);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ShouldThrowException_GivenNoPublicKeys()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp);

        // Assert
        Assert.Throws<InvalidOperationException>(() => sut.Validate(DialogToken));
    }

    [Fact]
    public void ShouldReturnError_GivenMalformedToken()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        // Act
        var result = sut.Validate("This.TokenIsMalformed....");

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid token format", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenInvalidToken()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        // Act
        var result = sut.Validate("This.TokenIs.Invalid");

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid token format", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenNoPublicKeyWithCorrectKeyId()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        var token = UpdateTokenHeader(DialogToken, "kid", "dp-testing-fake-kid");
        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid signature", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenExpiredToken()
    {
        // Arrange
        var sut = GetSut(
            DateTimeOffset.Parse("2025-02-17T09:00:00Z", CultureInfo.InvariantCulture),
            ValidPublicKeyPairs);

        // Act
        var result = sut.Validate(DialogToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid exp", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_WhenUsedBeforeNbf()
    {
        // Arrange
        var sut = GetSut(
            DateTimeOffset.Parse("2025-02-14T08:50:00Z", CultureInfo.InvariantCulture),
            ValidPublicKeyPairs);

        // Act
        var result = sut.Validate(DialogToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid nbf", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenEmptyToken()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        // Act
        var result = sut.Validate("");

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid token format", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenTokenWithWrongSignature()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        var token = UpdateTokenPayload(DialogToken, "l", "4");

        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid signature", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenTokenWithWrongAlg()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        var token = UpdateTokenHeader(DialogToken, "alg", "RS512");

        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid signature", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenMalformedJsonHeader()
    {
        var invalidHeader = """
                            {
                              "alg": "EdDSA",
                              "typ": "JWT",
                              "kid": "dp-staging-240322-o5ymn"
                            """u8;
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        var tokenParts = DialogToken.Split('.');
        tokenParts[0] = Base64Url.EncodeToString(invalidHeader);
        var token = string.Join(".", tokenParts);

        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid token format", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenMalformedJsonBody()
    {
        var invalidBody = """
                          {
                            "jti": "38cfdcb9-388b-47b8-a1bf-9f15b2819894",
                            "c": "urn:altinn:person:identifier-no:14886498226",
                          """u8;
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        var tokenParts = DialogToken.Split('.');
        tokenParts[1] = Base64Url.EncodeToString(invalidBody);
        var token = string.Join(".", tokenParts);

        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid token format", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnClaims_GivenValidTokenWithClaims()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);

        // Act
        var result = sut.Validate(DialogToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.ClaimsPrincipal);
        Assert.Equal("38cfdcb9-388b-47b8-a1bf-9f15b2819894", result.ClaimsPrincipal.FindFirst("jti")!.Value);
        Assert.Equal("urn:altinn:person:identifier-no:14886498226", result.ClaimsPrincipal.FindFirst("c")!.Value);
        Assert.Equal("3", result.ClaimsPrincipal.FindFirst("l")!.Value);
        Assert.Equal("urn:altinn:person:identifier-no:14886498226", result.ClaimsPrincipal.FindFirst("p")!.Value);
        Assert.Equal("urn:altinn:resource:dagl-correspondence", result.ClaimsPrincipal.FindFirst("s")!.Value);
        Assert.Equal("0194fe82-9280-77a5-a7cd-5ff0e6a6fa07", result.ClaimsPrincipal.FindFirst("i")!.Value);
        Assert.Equal("read", result.ClaimsPrincipal.FindFirst("a")!.Value);
        Assert.Equal("https://platform.tt02.altinn.no/dialogporten/api/v1", result.ClaimsPrincipal.FindFirst("iss")!.Value);
        Assert.Equal("1739523367", result.ClaimsPrincipal.FindFirst("iat")!.Value);
        Assert.Equal("1739523367", result.ClaimsPrincipal.FindFirst("nbf")!.Value);
        Assert.Equal("1739523967", result.ClaimsPrincipal.FindFirst("exp")!.Value);
    }

    [Fact]
    public void ClaimsShouldBeNull_GivenInvalidTokenFormat()
    {
        var invalidBody = """
                          {
                            "jti": "38cfdcb9-388b-47b8-a1bf-9f15b2819894",
                            "c": "urn:altinn:person:identifier-no:14886498226",
                          """u8;
        // Arrange
        var sut = GetSut(ValidTimeStamp, ValidPublicKeyPairs);
        var tokenParts = DialogToken.Split('.');
        tokenParts[1] = Base64Url.EncodeToString(invalidBody);
        var token = string.Join(".", tokenParts);

        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.Null(result.ClaimsPrincipal);
    }

    [Fact]
    public void ShouldReturnError_GivenTokenWithInvalidDialogId()
    {
        // Arrange
        var wrongDialogId = new Guid("329491ca-a4e9-4460-8988-f2dc80ea39fe");
        var sut = GetSut(ValidTimeStamp, publicKeyPairs: ValidPublicKeyPairs);

        // Act
        var result = sut.Validate(DialogToken, dialogId: wrongDialogId);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid dialog ID", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenTokenWithInvalidActions()
    {
        // Arrange
        var sut = GetSut(ValidTimeStamp, publicKeyPairs: ValidPublicKeyPairs);

        // Act
        var result = sut.Validate(DialogToken, requiredActions: ["write"]);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid actions", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenContextTokenType()
    {
        // Arrange
        var (token, keyPair) = CreateSignedToken(DialogTokenTypes.DialogContextToken);
        var sut = GetSut(ValidTimeStamp, keyPair);

        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid typ", result.Errors["token"]);
        Assert.DoesNotContain("Invalid signature", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnIsValid_GivenContextTokenTypeWhenAccepted()
    {
        // Arrange
        var (token, keyPair) = CreateSignedToken(DialogTokenTypes.DialogContextToken);
        var sut = GetSut(ValidTimeStamp, keyPair);

        // Act
        var result = sut.Validate(token, options: new DialogTokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            ValidTokenTypes = [DialogTokenTypes.DialogContextToken]
        });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ShouldReturnError_GivenTokenTypeOnlyInNestedProperty()
    {
        // A header carrying a "typ" only inside a nested object must not satisfy top-level typ validation.
        // Arrange
        const string kid = "dp-testing-generated-key";
        var header = JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["alg"] = "EdDSA",
            ["kid"] = kid,
            ["nested"] = new Dictionary<string, object> { ["typ"] = DialogTokenTypes.DialogToken }
        });
        var (token, keyPair) = CreateSignedTokenWithHeaderBytes(kid, header);
        var sut = GetSut(ValidTimeStamp, keyPair);

        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid typ", result.Errors["token"]);
        Assert.DoesNotContain("Invalid signature", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenNonObjectHeader()
    {
        // A header whose root is not a JSON object can never carry a valid top-level "typ".
        // Arrange
        const string kid = "dp-testing-generated-key";
        var header = JsonSerializer.SerializeToUtf8Bytes(NonObjectHeader);
        var (token, keyPair) = CreateSignedTokenWithHeaderBytes(kid, header);
        var sut = GetSut(ValidTimeStamp, keyPair);

        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid typ", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnError_GivenMissingTokenType()
    {
        // Arrange
        var (token, keyPair) = CreateSignedToken(tokenType: null);
        var sut = GetSut(ValidTimeStamp, keyPair);

        // Act
        var result = sut.Validate(token);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.ContainsKey("token"));
        Assert.Contains("Invalid typ", result.Errors["token"]);
    }

    [Fact]
    public void ShouldReturnIsValid_GivenContextTokenTypeWhenTypeValidationIsDisabled()
    {
        // Arrange
        var (token, keyPair) = CreateSignedToken(DialogTokenTypes.DialogContextToken);
        var sut = GetSut(ValidTimeStamp, keyPair);

        // Act
        var result = sut.Validate(token, options: new DialogTokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            ValidateTokenType = false
        });

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ShouldReturnIsValid_GivenTokenTypeEscapedInHeader()
    {
        // A JSON header may escape the '+' of a structured type as u002B. That is the same type.
        // Arrange
        var (token, keyPair) = CreateSignedToken(DialogTokenTypes.DialogContextToken, escapeHeader: true);
        var sut = GetSut(ValidTimeStamp, keyPair);
        Assert.Contains(@"dialogcontexttoken\u002Bjwt",
            Encoding.UTF8.GetString(Base64Url.DecodeFromChars(token.Split('.')[0])),
            StringComparison.Ordinal);

        // Act
        var result = sut.Validate(token, options: new DialogTokenValidationParameters
        {
            ClockSkew = TimeSpan.Zero,
            ValidTokenTypes = [DialogTokenTypes.DialogContextToken]
        });

        // Assert
        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Mints a token signed with a throwaway key, so the type assertions are made against a token that is
    /// valid in every other respect. Tampering with the header of <see cref="DialogToken"/> would invalidate
    /// its signature, which cannot be re-created without Dialogporten's private key.
    /// </summary>
    private static (string Token, PublicKeyPair KeyPair) CreateSignedToken(string? tokenType, bool escapeHeader = false)
    {
        const string kid = "dp-testing-generated-key";
        var header = tokenType is null
            ? new Dictionary<string, object> { ["alg"] = "EdDSA", ["kid"] = kid }
            : new Dictionary<string, object> { ["alg"] = "EdDSA", ["typ"] = tokenType, ["kid"] = kid };

        // Dialogporten writes the header with the relaxed encoder, leaving a structured type's '+' literal;
        // escapeHeader mints the equivalent header the default encoder would produce.
        var headerOptions = escapeHeader ? EscapingHeaderOptions : RelaxedHeaderOptions;
        return CreateSignedTokenWithHeaderBytes(kid, JsonSerializer.SerializeToUtf8Bytes(header, headerOptions));
    }

    /// <summary>
    /// Mints a token whose header is exactly the given raw JSON bytes, for exercising header shapes
    /// (nested properties, non-object roots) that can't be expressed via a flat header dictionary.
    /// The "kid" property, if present anywhere in <paramref name="headerBytes"/>, is not otherwise
    /// inspected here — the caller passes the matching <paramref name="kid"/> for key lookup.
    /// </summary>
    private static (string Token, PublicKeyPair KeyPair) CreateSignedTokenWithHeaderBytes(string kid, byte[] headerBytes)
    {
        using var key = Key.Create(SignatureAlgorithm.Ed25519, new KeyCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        });

        var claims = new Dictionary<string, object>
        {
            ["jti"] = "38cfdcb9-388b-47b8-a1bf-9f15b2819894",
            ["c"] = "urn:altinn:person:identifier-no:14886498226",
            ["l"] = 3,
            ["p"] = "urn:altinn:person:identifier-no:14886498226",
            ["s"] = "urn:altinn:resource:dagl-correspondence",
            ["i"] = "0194fe82-9280-77a5-a7cd-5ff0e6a6fa07",
            ["a"] = "read",
            ["iss"] = "https://platform.tt02.altinn.no/dialogporten/api/v1",
            ["iat"] = ValidTimeStamp.ToUnixTimeSeconds(),
            ["nbf"] = ValidTimeStamp.ToUnixTimeSeconds(),
            ["exp"] = ValidTimeStamp.AddMinutes(10).ToUnixTimeSeconds()
        };

        var signedPart = string.Join('.',
            Base64Url.EncodeToString(headerBytes),
            Base64Url.EncodeToString(JsonSerializer.SerializeToUtf8Bytes(claims)));
        var signature = SignatureAlgorithm.Ed25519.Sign(key, Encoding.UTF8.GetBytes(signedPart));

        var publicKeyPair = new PublicKeyPair(kid, key.PublicKey);
        return ($"{signedPart}.{Base64Url.EncodeToString(signature)}", publicKeyPair);
    }

    private static DialogTokenValidator GetSut(
        DateTimeOffset simulatedNow,
        params PublicKeyPair[] publicKeyPairs)
    {
        DialogTokenValidationParameters.Default.ClockSkew = TimeSpan.Zero;
        var keyCache = Substitute.For<IEdDsaSecurityKeysCache>();
        var clock = Substitute.For<IClock>();
        keyCache.PublicKeys.Returns(new ReadOnlyCollection<PublicKeyPair>(publicKeyPairs));
        clock.UtcNow.Returns(simulatedNow);
        return new DialogTokenValidator(keyCache, clock);
    }

    private static PublicKey ToPublicKey(string key)
        => PublicKey.Import(SignatureAlgorithm.Ed25519, Base64Url.DecodeFromChars(key), KeyBlobFormat.RawPublicKey);

    private static string UpdateTokenParts(string part, string property, string value)
    {
        var decodedPart = Base64Url.DecodeFromChars(part);
        var json = JsonSerializer.Deserialize<Dictionary<string, object>>(decodedPart)!;
        json[property] = value;
        var encodedPart = Base64Url.EncodeToUtf8(JsonSerializer.SerializeToUtf8Bytes(json));
        return Encoding.UTF8.GetString(encodedPart);
    }

    private static string UpdateTokenPayload(string token, string property, string value)
    {
        var tokenParts = token.Split('.');
        tokenParts[1] = UpdateTokenParts(tokenParts[1], property, value);
        return string.Join(".", tokenParts);
    }

    private static string UpdateTokenHeader(string token, string property, string value)
    {
        var tokenParts = token.Split('.');
        tokenParts[0] = UpdateTokenParts(tokenParts[0], property, value);
        return string.Join(".", tokenParts);
    }
}
