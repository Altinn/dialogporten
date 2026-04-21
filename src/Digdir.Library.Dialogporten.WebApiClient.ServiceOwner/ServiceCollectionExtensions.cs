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
}
