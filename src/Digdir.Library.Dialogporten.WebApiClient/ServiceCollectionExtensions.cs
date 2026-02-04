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

public static class ServiceCollectionExtensions
{
    private const string ClientDefinitionKey = "dialogporten-sp-sdk";

    public static IServiceCollection AddDialogportenClient(this IServiceCollection services, DialogportenSettings settings)
    {
        if (!DialogportenSettings.Validate())
        {
            throw new InvalidOperationException("Invalid configuration");
        }
        services.TryAddSingleton<IOptions<DialogportenSettings>>(new OptionsWrapper<DialogportenSettings>(settings));

        services.RegisterMaskinportenClientDefinition<SettingsJwkClientDefinition>(ClientDefinitionKey, settings.Maskinporten);

        var refitClients = AssemblyMarker.Assembly.GetTypes()
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
            services
                .AddRefitClient(refitClient, new RefitSettings
                {
                    ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
                })
                .ConfigureHttpClient(c => c.BaseAddress = new Uri(settings.BaseUri))
                .AddMaskinportenHttpMessageHandler<SettingsJwkClientDefinition>(ClientDefinitionKey);
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

    public static IServiceCollection AddDialogportenClient(this IServiceCollection services, Action<DialogportenSettings> configureOptions)
    {
        var dialogportenSettings = new DialogportenSettings();
        configureOptions.Invoke(dialogportenSettings);
        return services.AddDialogportenClient(dialogportenSettings);
    }
}
