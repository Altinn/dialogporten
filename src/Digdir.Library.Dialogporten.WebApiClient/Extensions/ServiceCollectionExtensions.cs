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
// using MaskinportenSettings = Altinn.Apiclient.Serviceowner.Config.MaskinportenSettings;

namespace Digdir.Library.Dialogporten.WebApiClient.Extensions;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection AddDialogTokenVerifer(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var dialogportenSettings = provider.GetRequiredService<IConfiguration>()
            .GetSection("Ed25519Keys")
            .Get<Ed25519Keys>();
        Console.WriteLine(dialogportenSettings);

        var keyPair = dialogportenSettings!.Primary;
        var kid = keyPair.Kid;

        var publicKey = PublicKey.Import(SignatureAlgorithm.Ed25519,
            Base64Url.Decode(keyPair.PublicComponent), KeyBlobFormat.RawPublicKey);
        services.AddSingleton(new DialogTokenVerifier(kid, publicKey));
        return services;
    }
    public static IServiceCollection AddDialogportenClient(this IServiceCollection services)
    {
        // Bygge en service provider for å få hentet ut settings         
        var provider = services.BuildServiceProvider();
        var dialogportenSettings = provider.GetRequiredService<IConfiguration>()
            .GetSection("DialogportenSettings")
            .Get<DialogportenSettings>();

        // Vi mapper denne til en Maskinporten setting
        var maskinportenSettings = new MaskinportenSettings()
        {
            EncodedJwk = dialogportenSettings!.Maskinporten.EncodedJwk,
            ClientId = dialogportenSettings.Maskinporten.ClientId,
            // Maskinportenmiljø utleded av Dialogporten-miljø
            Environment = dialogportenSettings.Environment == "prod" ? "prod" : "test",
            Scope = dialogportenSettings.Maskinporten.Scope,
        };

        // Vi registrerer en maskinporten klient med oppgite settings, som kan brukes som en http message handler
        services.RegisterMaskinportenClientDefinition<SettingsJwkClientDefinition>("dialogporten-sp-sdk", maskinportenSettings);

        var baseAddress = string.Empty;
        if (dialogportenSettings.Environment == "test")
        {
            baseAddress = "https://localhost:7214";
        }
        // Vi registrerer Refit, og legger til den registrerte maskinporten http message handlern
        // Amund: Partial er ikke mulig å finne etter compile time.
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

        // Vi registrerer vår egen API-abstraksjon, som selv tar inn og wrapper IDialgportenApi, som nå er "maskinporten-powered"
        // services.AddSingleton<IDialogportenClient, DialogportenClient>();

        return services;
    }
}
