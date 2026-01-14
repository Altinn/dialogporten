using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using Xunit;
using Xunit.Sdk;
using static System.Text.Json.Serialization.JsonIgnoreCondition;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Common;

public class GraphQlE2EFixture : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private ITokenOverridesAccessor? _tokenOverridesAccessor;

    private PreflightState PreflightState { get; set; } = null!;

    protected IDialogportenGraphQlTestClient GraphQlClient { get; private set; } = null!;
    protected IServiceownerApi ServiceownerApi { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Development;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<E2ESettings>(optional: true)
            .Build();

        var settings = configuration.Get<E2ESettings>()
            ?? throw new InvalidOperationException("E2E settings are missing.");

        var services = new ServiceCollection();

        services.AddHttpClient<TestTokenHandler>();
        services.AddSingleton<ITokenOverridesAccessor, TokenOverridesAccessor>();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddOptions();
        services.Configure<E2ESettings>(configuration);

        var jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = WhenWritingNull
        };

        var webApiUri = new UriBuilder(settings.DialogportenBaseUri)
        {
            Port = settings.WebAPiPort
        }.Uri;

        services
            .AddRefitClient<IServiceownerApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
            })
            .ConfigureHttpClient(httpClient => httpClient.BaseAddress = webApiUri)
            .AddHttpMessageHandler(serviceProvider =>
                ActivatorUtilities.CreateInstance<TestTokenHandler>(serviceProvider, TokenKind.ServiceOwner));

        var graphQlBaseAddress = settings.GraphQlPort is -1
            ? settings.DialogportenBaseUri
            : settings.DialogportenBaseUri.Replace("https", "http");

        var graphQlPath = environment == Environments.Development ? "/graphql" : "/dialogporten/graphql";
        var graphQlUri = new UriBuilder(graphQlBaseAddress)
        {
            Path = graphQlPath,
            Port = settings.GraphQlPort
        }.Uri;

        services
            .AddDialogportenGraphQlTestClient()
            .ConfigureHttpClient(x => x.BaseAddress = graphQlUri,
                builder => builder.AddHttpMessageHandler(serviceProvider =>
                    ActivatorUtilities.CreateInstance<TestTokenHandler>(serviceProvider, TokenKind.EndUser)));

        _serviceProvider = services.BuildServiceProvider();

        GraphQlClient = _serviceProvider.GetRequiredService<IDialogportenGraphQlTestClient>();
        ServiceownerApi = _serviceProvider.GetRequiredService<IServiceownerApi>();
        _tokenOverridesAccessor = _serviceProvider.GetRequiredService<ITokenOverridesAccessor>();

        var preflight = await CreatePreflightState(graphQlUri, webApiUri);
        PreflightState = preflight;
    }

    public ValueTask DisposeAsync()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public IDisposable UseTokenOverrides(TokenOverrides overrides) =>
        _tokenOverridesAccessor is null
            ? throw new InvalidOperationException("Token override accessor not initialized.")
            : new TokenOverrideScope(_tokenOverridesAccessor, overrides);

    public IDisposable UseEndUserTokenOverrides(
        string? ssn = null,
        string? scopes = null,
        string? tokenOverride = null) =>
        UseTokenOverrides(new TokenOverrides(
            EndUser: new EndUserTokenOverrides(ssn, scopes, tokenOverride)));

    public IDisposable UseServiceOwnerTokenOverrides(
        string? orgNumber = null,
        string? orgName = null,
        string? scopes = null,
        string? tokenOverride = null) =>
        UseTokenOverrides(new TokenOverrides(
            ServiceOwner: new ServiceOwnerTokenOverrides(orgNumber, orgName, scopes, tokenOverride)));

    protected void PreflightCheck()
    {
        var issues = new List<string>();

        if (PreflightState.GraphQlError is not null)
        {
            issues.Add($"GraphQL not reachable at {PreflightState.GraphQlUri}. Error: {PreflightState.GraphQlError}");
        }

        if (PreflightState.WebApiError is not null)
        {
            issues.Add($"WebAPI not reachable at {PreflightState.WebApiUri}. Error: {PreflightState.WebApiError}");
        }

        if (issues.Count != 0)
        {
            throw SkipException.ForSkip($"GraphQL E2E preflight failed: {Environment.NewLine} {string.Join("; ", issues)}");
        }
    }

    private static async Task<PreflightState> CreatePreflightState(Uri graphQlUri, Uri webApiUri)
    {
        using var httpClient = new HttpClient();

        var graphQlError = await TryPing(httpClient, graphQlUri);
        var webApiError = await TryPing(httpClient, webApiUri);

        return new PreflightState(
            GraphQlUri: graphQlUri,
            WebApiUri: webApiUri,
            GraphQlError: graphQlError,
            WebApiError: webApiError);
    }

    private static async Task<string?> TryPing(HttpClient httpClient, Uri uri)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);

        try
        {
            using var result = await httpClient.SendAsync(request);
            result.EnsureSuccessStatusCode();
            return null;
        }
        catch (Exception exception)
        {
            return exception.GetBaseException().Message;
        }
    }
}

public sealed record PreflightState(
    Uri GraphQlUri,
    Uri WebApiUri,
    string? GraphQlError,
    string? WebApiError);
