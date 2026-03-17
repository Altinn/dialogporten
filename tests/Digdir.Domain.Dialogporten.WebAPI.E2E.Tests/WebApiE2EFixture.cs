using System.Text.Json;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1;
using Digdir.Library.Dialogporten.E2E.Common;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using static System.Text.Json.Serialization.JsonIgnoreCondition;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests;

public sealed class WebApiE2EFixture : E2EFixtureBase
{
    public IEnduserApi EnduserApi { get; private set; } = null!;
    public IEnduserApi SystemUserEnduserApi { get; private set; } = null!;

    private Uri _webApiUri = null!;
    private RefitSettings _refitSettings = null!;

    protected override bool IncludeGraphQlPreflight => false;

    protected override void ConfigureServices(
        IServiceCollection services,
        E2ESettings settings,
        Uri webApiUri,
        Uri graphQlUri)
    {
        _webApiUri = webApiUri;

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        _refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
        };

        services
            .AddRefitClient<IEnduserApi>(_refitSettings)
            .ConfigureHttpClient(httpClient => httpClient.BaseAddress = webApiUri)
            .AddHttpMessageHandler(serviceProvider =>
                ActivatorUtilities.CreateInstance<TestTokenHandler>(serviceProvider, TokenKind.EndUser));
    }

    protected override void AfterServiceProviderBuilt(ServiceProvider serviceProvider)
    {
        EnduserApi = serviceProvider.GetRequiredService<IEnduserApi>();

        // Manually create SystemUser client to avoid DI conflicts
        var systemUserTokenHandler = ActivatorUtilities.CreateInstance<TestTokenHandler>(serviceProvider, TokenKind.SystemUser);
        var systemUserHttpClient = new HttpClient(systemUserTokenHandler)
        {
            BaseAddress = _webApiUri
        };
        SystemUserEnduserApi = RestService.For<IEnduserApi>(systemUserHttpClient, _refitSettings);
    }
}
