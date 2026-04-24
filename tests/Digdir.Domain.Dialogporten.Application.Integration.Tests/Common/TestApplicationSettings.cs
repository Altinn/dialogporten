using Digdir.Domain.Dialogporten.Application;
using Microsoft.Extensions.Options;
using NSec.Cryptography;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal sealed class TestApplicationSettings : IOptionsSnapshot<ApplicationSettings>
{
    private static ApplicationSettings _default = CreateDefaultWithGeneratedKeys();

    public ApplicationSettings Value { get; private set; } = _default;

    public ApplicationSettings Get(string? name) => Value;

    public void Set(ApplicationSettings value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }

    public void Reset(bool regenerate = false)
    {
        if (regenerate)
        {
            _default = CreateDefaultWithGeneratedKeys();
        }

        Value = _default;
    }

    public static ApplicationSettings CreateDefault(
        FeatureToggle? featureToggle = null,
        DialogportenSettings? dialogporten = null,
        LimitsSettings? limits = null,
        BadDataHandling? badDataHandling = null) =>
        new()
        {
            FeatureToggle = featureToggle ?? _default.FeatureToggle,
            Dialogporten = dialogporten ?? _default.Dialogporten,
            Limits = limits ?? _default.Limits,
            BadDataHandling = badDataHandling ?? _default.BadDataHandling
        };

    private static ApplicationSettings CreateDefaultWithGeneratedKeys() =>
        new()
        {
            FeatureToggle = new FeatureToggle
            {
                UseAltinnAutoAuthorizedPartiesQueryParameters = true,
                UseCorrectPersonNameOrdering = true
            },
            Dialogporten = CreateDefaultDialogportenSettings(),
            Limits = new LimitsSettings(),
            BadDataHandling = BadDataHandling.WarnAndContinue
        };

    private static DialogportenSettings CreateDefaultDialogportenSettings() =>
        new()
        {
            BaseUri = new Uri("https://integration.test"),
            Ed25519KeyPairs = new Ed25519KeyPairs
            {
                Primary = CreateKeyPair("primary"),
                Secondary = CreateKeyPair("secondary")
            }
        };

    private static Ed25519KeyPair CreateKeyPair(string name)
    {
        using var keyPair = Key.Create(SignatureAlgorithm.Ed25519,
            new KeyCreationParameters
            {
                ExportPolicy = KeyExportPolicies.AllowPlaintextExport
            });

        return new()
        {
            Kid = $"integration-test-{name}-signing-key",
            PrivateComponent = Base64UrlEncode(keyPair.Export(KeyBlobFormat.RawPrivateKey)),
            PublicComponent = Base64UrlEncode(keyPair.Export(KeyBlobFormat.RawPublicKey))
        };
    }

    private static string Base64UrlEncode(byte[] input) =>
        Convert.ToBase64String(input)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
}
