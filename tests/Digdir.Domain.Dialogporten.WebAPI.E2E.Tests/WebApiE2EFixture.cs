using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using Digdir.Library.Dialogporten.E2E.Common;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using static System.Text.Json.Serialization.JsonIgnoreCondition;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests;

public sealed class WebApiE2EFixture : E2EFixtureBase
{
    public IEndUserApi EnduserApi { get; private set; } = null!;

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

        services
            .AddRefitClient<IEnduserApi>(refitSettings)
            .ConfigureHttpClient(httpClient => httpClient.BaseAddress = webApiUri)
            .AddHttpMessageHandler(serviceProvider =>
                ActivatorUtilities.CreateInstance<TestTokenHandler>(serviceProvider, TokenKind.EndUser));

        services.AddTransient<IEndUserApi, EndUserApi>();
    }

    protected override void AfterServiceProviderBuilt(ServiceProvider serviceProvider)
    {
        EnduserApi = serviceProvider.GetRequiredService<IEndUserApi>();
    }
}
