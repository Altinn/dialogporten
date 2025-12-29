using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using Xunit;
using static System.Text.Json.Serialization.JsonIgnoreCondition;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;

public class GraphQlE2EFixture : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;

    public IDialogportenGraphQlTestClient GraphQlClient { get; private set; } = null!;
    public IServiceownerApi ServiceownerApi { get; private set; } = null!;

    public ValueTask InitializeAsync()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        environment.Should().NotBeNull();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<E2ESettings>(optional: true)
            .Build();

        var settings = configuration.Get<E2ESettings>();
        settings.Should().NotBeNull();

        var services = new ServiceCollection();

        services.AddHttpClient<ServiceOwnerTokenHandler>();
        services.AddHttpClient<EndUserTokenHandler>();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.Configure<E2ESettings>(configuration);

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = WhenWritingNull
        };

        services
            .AddSingleton<ServiceOwnerTokenHandler>()
            .AddRefitClient<IServiceownerApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
            })
            .ConfigureHttpClient(httpClient => httpClient.BaseAddress =
                new UriBuilder(settings.DialogportenBaseUri)
                {
                    Port = settings.WebAPiPort
                }.Uri)
            .AddHttpMessageHandler<ServiceOwnerTokenHandler>();

        var graphQlBaseAddress = settings.GraphQlPort is -1
            ? settings.DialogportenBaseUri
            : settings.DialogportenBaseUri.Replace("https", "http");

        services
            .AddSingleton<EndUserTokenHandler>()
            .AddDialogportenGraphQlTestClient()
            .ConfigureHttpClient(x => x.BaseAddress =
                    new UriBuilder(graphQlBaseAddress)
                    {
                        Path = "/graphql",
                        Port = settings.GraphQlPort
                    }.Uri,
                builder => builder.AddHttpMessageHandler<EndUserTokenHandler>());

        _serviceProvider = services.BuildServiceProvider();

        GraphQlClient = _serviceProvider.GetRequiredService<IDialogportenGraphQlTestClient>();
        ServiceownerApi = _serviceProvider.GetRequiredService<IServiceownerApi>();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
