using System.CodeDom.Compiler;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using Altinn.ApiClients.Dialogporten.ServiceOwner.V1;
using Digdir.Library.Dialogporten.E2E.Common;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using static System.Text.Json.Serialization.JsonIgnoreCondition;

namespace Digdir.Domain.Dialogporten.WebAPI.ServiceOwner.E2E.Tests;

public sealed class WebApiServiceOwnerE2EFixture : E2EFixtureBase
{
    public IServiceOwnerClient ServiceOwnerClient { get; private set; } = null!;

    protected override bool IncludeGraphQlPreflight => false;

    protected override void ConfigureServices(
        IServiceCollection services,
        E2ESettings settings,
        Uri webApiUri,
        Uri graphQlUri)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
        };

        var refitClients = typeof(IServiceOwnerClient).Assembly
            .GetTypes()
            .Where(static type =>
                type is { IsInterface: true } &&
                type.GetCustomAttribute<GeneratedCodeAttribute>()?.Tool == "Refitter")
            .ToList();

        foreach (var refitClient in refitClients)
        {
            services
                .AddRefitClient(refitClient, refitSettings)
                .ConfigureHttpClient(httpClient => httpClient.BaseAddress = webApiUri)
                .AddHttpMessageHandler(serviceProvider =>
                    ActivatorUtilities.CreateInstance<TestTokenHandler>(serviceProvider, TokenKind.ServiceOwner));
        }

        services.AddTransient<IServiceOwnerV1, ServiceOwnerV1>();
        services.Decorate<IServiceOwnerV1, EphemeralServiceOwnerV1Decorator>();
        services.AddTransient<IServiceOwnerClient, ServiceOwnerClient>();
    }

    protected override void AfterServiceProviderBuilt(ServiceProvider serviceProvider) =>
        ServiceOwnerClient = serviceProvider.GetRequiredService<IServiceOwnerClient>();
}
