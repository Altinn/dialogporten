using System.Text.Json;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Interfaces.V1;
using Digdir.Library.Dialogporten.E2E.Common;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using static System.Text.Json.Serialization.JsonIgnoreCondition;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests;

public sealed class WebApiE2EFixture : E2EFixtureBase
{
    public IEnduserApi EnduserApi { get; private set; } = null!;

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

        services
            .AddRefitClient<IEnduserApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
            })
            .ConfigureHttpClient(httpClient => httpClient.BaseAddress = webApiUri)
            .AddHttpMessageHandler(serviceProvider =>
                ActivatorUtilities.CreateInstance<TestTokenHandler>(serviceProvider, TokenKind.EndUser));
    }

    protected override void AfterServiceProviderBuilt(ServiceProvider serviceProvider) =>
        EnduserApi = serviceProvider.GetRequiredService<IEnduserApi>();
}
