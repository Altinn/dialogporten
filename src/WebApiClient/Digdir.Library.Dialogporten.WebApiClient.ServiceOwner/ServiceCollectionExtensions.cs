using Microsoft.Extensions.DependencyInjection;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

public static class ServiceCollectionExtensions
{
    private const string ClientDefinitionKey = "dialogporten-sp-sdk-serviceowner";

    public static IServiceCollection AddDialogportenClient(this IServiceCollection services, DialogportenSettings settings)
        => services.AddDialogportenClientCore<IServiceOwnerApi, ServiceOwnerApi>(settings, ClientDefinitionKey, AssemblyMarker.Assembly);

    public static IServiceCollection AddDialogportenClient(this IServiceCollection services, Action<DialogportenSettings> configureOptions)
    {
        var dialogportenSettings = new DialogportenSettings();
        configureOptions.Invoke(dialogportenSettings);
        return services.AddDialogportenClient(dialogportenSettings);
    }

    /// <summary>
    /// Registers the Dialogporten service owner client without configuring any authentication.
    /// The Dialogporten <see cref="DialogportenSettings.Maskinporten"/> settings are not used; the caller is
    /// responsible for attaching authentication (e.g. Maskinporten) to each underlying Refit client via
    /// <paramref name="configureAuthentication"/>.
    /// </summary>
    public static IServiceCollection AddDialogportenClient(this IServiceCollection services, DialogportenSettings settings, Action<IHttpClientBuilder> configureAuthentication)
        => services.AddDialogportenClientCore<IServiceOwnerApi, ServiceOwnerApi>(settings, ClientDefinitionKey, AssemblyMarker.Assembly, configureAuthentication);

    /// <inheritdoc cref="AddDialogportenClient(IServiceCollection, DialogportenSettings, Action{IHttpClientBuilder})"/>
    public static IServiceCollection AddDialogportenClient(this IServiceCollection services, Action<DialogportenSettings> configureOptions, Action<IHttpClientBuilder> configureAuthentication)
    {
        var dialogportenSettings = new DialogportenSettings();
        configureOptions.Invoke(dialogportenSettings);
        return services.AddDialogportenClient(dialogportenSettings, configureAuthentication);
    }
}
