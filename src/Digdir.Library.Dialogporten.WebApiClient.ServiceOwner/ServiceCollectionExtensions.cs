using System.CodeDom.Compiler;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Refit;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

public static class ServiceCollectionExtensions
{
    // Must match the key used in the base WebApiClient project.
    private const string ClientDefinitionKey = "dialogporten-sp-sdk";

    /// <summary>
    /// Registers the Dialogporten service owner client and all its dependencies.
    /// </summary>
    public static IServiceCollection AddDialogportenServiceOwnerClient(
        this IServiceCollection services,
        DialogportenSettings settings)
    {
        // Register shared infrastructure from the base project (idempotent via TryAdd).
        services.AddDialogportenClient(settings);

        RegisterRefitClients(services, settings);
        RegisterFacade(services);

        return services;
    }

    /// <summary>
    /// Registers the Dialogporten service owner client and all its dependencies.
    /// </summary>
    public static IServiceCollection AddDialogportenServiceOwnerClient(
        this IServiceCollection services,
        Action<DialogportenSettings> configureOptions)
    {
        var settings = new DialogportenSettings();
        configureOptions.Invoke(settings);
        return services.AddDialogportenServiceOwnerClient(settings);
    }

    private static void RegisterRefitClients(IServiceCollection services, DialogportenSettings settings)
    {
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
    }

    private static void RegisterFacade(IServiceCollection services)
    {
        services.TryAddTransient<IDialogportenServiceOwnerV1, DialogportenServiceOwnerV1>();
        services.TryAddTransient<IDialogportenServiceOwnerClient, DialogportenServiceOwnerClient>();
    }
}
