using System.CodeDom.Compiler;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.Common;
using Altinn.ApiClients.Dialogporten.Infrastructure;
using Altinn.ApiClients.Dialogporten.Services;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Refit;

namespace Altinn.ApiClients.Dialogporten;

internal static class DialogportenServiceCollectionExtensions
{
    internal static IServiceCollection AddDialogportenClientCore(
        this IServiceCollection services,
        DialogportenSettings settings,
        string clientDefinitionKey,
        Assembly clientAssembly,
        Action<IHttpClientBuilder>? configureAuthentication = null)
    {
        if (!DialogportenSettings.Validate())
        {
            throw new InvalidOperationException("Invalid configuration");
        }

        services.TryAddSingleton<IOptions<DialogportenSettings>>(new OptionsWrapper<DialogportenSettings>(settings));

        // When the caller supplies their own authentication setup we do not wire up Maskinporten at all;
        // the caller is responsible for attaching the desired auth (e.g. Maskinporten) to each Refit client.
        if (configureAuthentication is null)
        {
            services.RegisterMaskinportenClientDefinition<SettingsJwkClientDefinition>(clientDefinitionKey, settings.Maskinporten
                ?? throw new InvalidOperationException(
                    $"{nameof(DialogportenSettings)}.{nameof(DialogportenSettings.Maskinporten)} must be set when no authentication is configured."));
        }

        var refitClients = clientAssembly.GetTypes()
            .Where(x =>
                x.IsInterface &&
                x.GetCustomAttribute<GeneratedCodeAttribute>()?.Tool == "Refitter")
            .ToList();

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        foreach (var refitClient in refitClients)
        {
            var clientBuilder = services
                .AddRefitClient(refitClient, new RefitSettings
                {
                    ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
                })
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(settings.BaseUri));

            if (configureAuthentication is null)
            {
                clientBuilder.AddMaskinportenHttpMessageHandler<SettingsJwkClientDefinition>(clientDefinitionKey);
            }
            else
            {
                configureAuthentication(clientBuilder);
            }
        }

        services
            .AddRefitClient<IInternalDialogportenApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(settings.BaseUri));

        services.AddHostedService<EdDsaSecurityKeysCacheService>();
        services.TryAddTransient<IDialogTokenValidator, DialogTokenValidator>();
        services.TryAddSingleton<DefaultEdDsaSecurityKeysCache>();
        services.TryAddSingleton<IEdDsaSecurityKeysCache>(x => x.GetRequiredService<DefaultEdDsaSecurityKeysCache>());
        services.TryAddTransient<IClock, DefaultClock>();

        return services;
    }

    internal static IServiceCollection AddDialogportenClientCore<TRootApi, TRootApiImplementation>(
        this IServiceCollection services,
        DialogportenSettings settings,
        string clientDefinitionKey,
        Assembly clientAssembly,
        Action<IHttpClientBuilder>? configureAuthentication = null)
        where TRootApi : class
        where TRootApiImplementation : class, TRootApi
    {
        services.AddDialogportenClientCore(settings, clientDefinitionKey, clientAssembly, configureAuthentication);
        services.TryAddTransient<TRootApi, TRootApiImplementation>();
        return services;
    }
}
