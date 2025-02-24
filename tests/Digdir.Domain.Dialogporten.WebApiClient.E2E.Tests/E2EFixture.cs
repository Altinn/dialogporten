using Altinn.ApiClients.Dialogporten;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Digdir.Domain.Dialogporten.WebApiClient.E2E.Tests;

public sealed class E2EFixture : TestBedFixture
{
    private static string _clientId = "";
    private static string _encodedJwk = "";

    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
    {
        var settings = configuration?
            .GetSection(nameof(DialogportenSettings))
            .Get<DialogportenSettings>()!;

        settings.Maskinporten.ClientId = _clientId;
        settings.Maskinporten.EncodedJwk = _encodedJwk;

        services.AddDialogportenClient(settings);
    }

    protected override ValueTask DisposeAsyncCore() => new();

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new() { Filename = "appsettings.json", IsOptional = false };
    }

    protected override void AddUserSecrets(IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddUserSecrets<E2EFixture>(optional: true);

        var config = configurationBuilder.Build();
        _clientId = config["DialogportenSettings:Maskinporten:ClientId"]!;
        _encodedJwk = config["DialogportenSettings:Maskinporten:EncodedJwk"]!;
    }
}
