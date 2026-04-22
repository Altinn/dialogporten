using Microsoft.Extensions.DependencyInjection;

namespace Altinn.ApiClients.Dialogporten;

public static class ServiceCollectionExtensions
{
    private const string ClientDefinitionKey = "dialogporten-sp-sdk";

    public static IServiceCollection AddDialogportenClient(this IServiceCollection services, DialogportenSettings settings)
        => services.AddDialogportenClientCore(settings, ClientDefinitionKey, AssemblyMarker.Assembly);

    public static IServiceCollection AddDialogportenClient(this IServiceCollection services, Action<DialogportenSettings> configureOptions)
    {
        var dialogportenSettings = new DialogportenSettings();
        configureOptions.Invoke(dialogportenSettings);
        return services.AddDialogportenClient(dialogportenSettings);
    }
}
