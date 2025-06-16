using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.Common;
using Altinn.ApiClients.Dialogporten.Services;
using BenchmarkDotNet.Attributes;
using NSec.Cryptography;
using Base64Url = System.Buffers.Text.Base64Url;

namespace Digdir.Tool.Dialogporten.Benchmarks;

[MemoryDiagnoser]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class TokenValidatorBenchmarks
{
    private const string ValidTimeStampString = "2025-02-14T09:00:00Z";
    private const string DialogToken =
        "eyJhbGciOiJFZERTQSIsInR5cCI6IkpXVCIsImtpZCI6ImRwLXN0YWdpbmctMjQwMzIyLW81eW1uIn0.eyJqdGkiOiIzOGNmZGNiOS0zODhiLTQ3YjgtYTFiZi05ZjE1YjI4MTk4OTQiLCJjIjoidXJuOmFsdGlubjpwZXJzb246aWRlbnRpZmllci1ubzoxNDg4NjQ5ODIyNiIsImwiOjMsInAiOiJ1cm46YWx0aW5uOnBlcnNvbjppZGVudGlmaWVyLW5vOjE0ODg2NDk4MjI2IiwicyI6InVybjphbHRpbm46cmVzb3VyY2U6ZGFnbC1jb3JyZXNwb25kZW5jZSIsImkiOiIwMTk0ZmU4Mi05MjgwLTc3YTUtYTdjZC01ZmYwZTZhNmZhMDciLCJhIjoicmVhZCIsImlzcyI6Imh0dHBzOi8vcGxhdGZvcm0udHQwMi5hbHRpbm4ubm8vZGlhbG9ncG9ydGVuL2FwaS92MSIsImlhdCI6MTczOTUyMzM2NywibmJmIjoxNzM5NTIzMzY3LCJleHAiOjE3Mzk1MjM5Njd9.O_f-RJhRPT7B76S7aOGw6jfxKDki3uJQLLC8nVlcNVJWFIOQUsy6gU4bG1ZdqoMBZPvb2K2X4I5fGpHW9dQMAA";
    private static readonly PublicKeyPair[] ValidPublicKeyPairs =
    [
        new("dp-staging-240322-o5ymn", ToPublicKey("zs9hR9oqgf53th2lTdrBq3C1TZ9UlR-HVJOiUpWV63o")),
        new("dp-staging-240322-rju3g", ToPublicKey("23Sijekv5ATW4sSEiRPzL_rXH-zRV8MK8jcs5ExCmSU"))
    ];
    private static readonly DialogTokenValidator _sut = new(
        new DefaultEdDsaSecurityKeysCache(ValidPublicKeyPairs),
        new BenchmarkClock(ValidTimeStampString));


    [Benchmark]
    public IValidationResult ValidateDialogToken() => _sut.Validate(DialogToken);

    private static PublicKey ToPublicKey(string key)
        => PublicKey.Import(SignatureAlgorithm.Ed25519, Base64Url.DecodeFromChars(key), KeyBlobFormat.RawPublicKey);

    private sealed class BenchmarkClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; } = utcNow;
        public BenchmarkClock(string input) : this(DateTimeOffset.Parse(input, CultureInfo.InvariantCulture)) { }
    }
}

