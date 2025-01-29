using System.Buffers.Text;
using System.Reflection;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Services;
using Digdir.Library.Dialogporten.WebApiClient.Config;
using Digdir.Library.Dialogporten.WebApiClient.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSec.Cryptography;
using Refit;
using MaskinportenSettings = Altinn.ApiClients.Maskinporten.Config.MaskinportenSettings;

namespace Digdir.Library.Dialogporten.WebApiClient.Extensions;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddDialogTokenVerifer(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var dialogportenSettings = provider.GetRequiredService<IConfiguration>()
            .GetSection("Ed25519Keys")
            .Get<Ed25519Keys>();

        var keyPair = dialogportenSettings!.Primary;
        var kid = keyPair.Kid;
        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519,
            Base64Url.DecodeFromChars(keyPair.PublicComponent), KeyBlobFormat.RawPublicKey);
        services.AddSingleton(new DialogTokenVerifier(kid, publicKey));
        return services;
    }
    public static IServiceCollection AddDialogportenClient(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var dialogportenSettings = provider.GetRequiredService<IConfiguration>()
            .GetSection("DialogportenSettings")
            .Get<DialogportenSettings>();

        var maskinportenSettings = new MaskinportenSettings()
        {
            EncodedJwk = dialogportenSettings!.Maskinporten.EncodedJwk,
            ClientId = dialogportenSettings.Maskinporten.ClientId,
            Environment = dialogportenSettings.Environment switch
            {
                "prod" => "prod",
                "local" => "test",
                "test" => "test",
                _ => throw new ArgumentException()
            },
            Scope = dialogportenSettings.Maskinporten.Scope
        };

        services.RegisterMaskinportenClientDefinition<SettingsJwkClientDefinition>("dialogporten-sp-sdk", maskinportenSettings);

        var baseAddress = dialogportenSettings.Environment switch
        {
            "test" => "https://platform.tt02.altinn.no/dialogporten",
            "local" => "https://localhost:7214",
            "prod" => "https://platform.altinn.no/dialogporten",
            _ => throw new NotImplementedException()
        };
        var refitClients = Assembly.GetExecutingAssembly().GetTypes()
            .Where(x =>
                x.Namespace!.StartsWith("Digdir.Library.Dialogporten.WebApiClient.Features.V1", StringComparison.InvariantCulture) &&
                x.IsInterface)
            .ToList();

        foreach (var refitClient in refitClients)
        {
            services
                .AddRefitClient(refitClient)
                .ConfigureHttpClient(c =>
                {
                    c.BaseAddress = new Uri(baseAddress);
                })
                .AddMaskinportenHttpMessageHandler<SettingsJwkClientDefinition>("dialogporten-sp-sdk");
        }

        return services;
    }
}
