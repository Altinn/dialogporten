using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace Digdir.Domain.Dialogporten.Application.Common;

public interface ICompactJwsGenerator
{
    string GetCompactJws(Dictionary<string, object?> claims, string tokenType);
    bool VerifyCompactJws(string compactJws);
}

public sealed class Ed25519Generator : ICompactJwsGenerator
{
    private readonly ApplicationSettings _applicationSettings;
    private string? _kid;
    private Key? _privateKey;
    private PublicKey? _publicKey;

    public Ed25519Generator(IOptions<ApplicationSettings> applicationSettings)
    {
        _applicationSettings = applicationSettings.Value;
        InitSigningKey();
    }

    public string GetCompactJws(Dictionary<string, object?> claims, string tokenType)
    {
        var header = JsonSerializer.SerializeToUtf8Bytes(new
        {
            alg = "EdDSA",
            typ = tokenType,
            kid = _kid
        });

        var payload = JsonSerializer.SerializeToUtf8Bytes(claims);

        var bufferSize = Base64Url.GetMaxEncodedToUtf8Length(header.Length) +
                         Base64Url.GetMaxEncodedToUtf8Length(payload.Length) +
                         Base64Url.GetMaxEncodedToUtf8Length(SignatureAlgorithm.Ed25519.SignatureSize) +
                         2; // For the dots

        Span<byte> buffer = stackalloc byte[bufferSize];

        Base64Url.Encode(header, buffer, out var bytesWritten);
        var dotIndex = bytesWritten;
        buffer[dotIndex] = (byte)'.';

        Base64Url.Encode(payload, buffer[(dotIndex + 1)..], out bytesWritten);
        dotIndex += bytesWritten + 1;
        buffer[dotIndex] = (byte)'.';

        var dataToSign = Encoding.UTF8.GetString(buffer[..dotIndex]);
        var signature = SignatureAlgorithm.Ed25519.Sign(_privateKey!, Encoding.UTF8.GetBytes(dataToSign));

        Base64Url.Encode(signature, buffer[(dotIndex + 1)..], out bytesWritten);

        return Encoding.UTF8.GetString(buffer[..(dotIndex + 1 + bytesWritten)]);
    }

    public bool VerifyCompactJws(string compactJws)
    {
        var parts = compactJws.Split('.');
        if (parts.Length != 3) return false;

        var header = Base64Url.Decode(parts[0]);

        var headerJson = JsonSerializer.Deserialize<JsonElement>(header);
        if (headerJson.TryGetProperty("kid", out var kid))
        {
            if (kid.GetString() != _kid) return false;
        }
        else
        {
            return false;
        }

        var signature = Base64Url.Decode(parts[2]);
        return SignatureAlgorithm.Ed25519.Verify(_publicKey!, Encoding.UTF8.GetBytes(parts[0] + '.' + parts[1]), signature);
    }

    private void InitSigningKey()
    {
        if (_kid != null) return;

        var keyPair = _applicationSettings.Dialogporten.Ed25519KeyPairs.Primary;
        _kid = keyPair.Kid;
        _privateKey = Key.Import(SignatureAlgorithm.Ed25519,
            Base64Url.Decode(keyPair.PrivateComponent), KeyBlobFormat.RawPrivateKey);
        _publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519,
            Base64Url.Decode(keyPair.PublicComponent), KeyBlobFormat.RawPublicKey);
    }
}

internal sealed class LocalDevelopmentCompactJwsGeneratorDecorator : ICompactJwsGenerator
{
    // ReSharper disable once UnusedParameter.Local
    public LocalDevelopmentCompactJwsGeneratorDecorator(ICompactJwsGenerator _)
    {
    }

    public string GetCompactJws(Dictionary<string, object?> claims, string tokenType) => "local-development-jws";

    public bool VerifyCompactJws(string compactJws) => true;
}

public static class Base64Url
{
    public static int GetMaxEncodedToUtf8Length(int length) => (length + 2) / 3 * 4;

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

    public static byte[] Decode(string input)
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
