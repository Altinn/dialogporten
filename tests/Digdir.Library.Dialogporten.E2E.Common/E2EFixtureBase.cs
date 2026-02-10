using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using Xunit;
using static System.Text.Json.Serialization.JsonIgnoreCondition;

namespace Digdir.Library.Dialogporten.E2E.Common;

public abstract class E2EFixtureBase : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private ITokenOverridesAccessor? _tokenOverridesAccessor;

    private PreflightState? PreflightState { get; set; }

    public IServiceownerApi ServiceownerApi { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Development;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets<E2ESettings>(optional: true)
            .AddEnvironmentVariables()
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
            DefaultIgnoreCondition = WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
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

        services.Decorate<IServiceownerApi, EphemeralDialogDecorator>();

        var graphQlPath = environment == Environments.Development ? "/graphql" : "/dialogporten/graphql";
        var graphQlUriBuilder = new UriBuilder(settings.DialogportenBaseUri)
        {
            Path = graphQlPath
        };

        if (settings.GraphQlPort is not -1)
        {
            graphQlUriBuilder.Scheme = "http";
            graphQlUriBuilder.Port = settings.GraphQlPort;
        }

        var graphQlUri = graphQlUriBuilder.Uri;

        ConfigureServices(services, settings, webApiUri, graphQlUri);

        _serviceProvider = services.BuildServiceProvider();

        ServiceownerApi = _serviceProvider.GetRequiredService<IServiceownerApi>();
        _tokenOverridesAccessor = _serviceProvider.GetRequiredService<ITokenOverridesAccessor>();

        AfterServiceProviderBuilt(_serviceProvider);

        PreflightState = await CreatePreflightState(graphQlUri, webApiUri);
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

    public void Cleanup() => ClearTokenOverrides();

    public void PreflightCheck()
    {
        var preFlightIssues = new List<string>();

        if (PreflightState is null)
        {
            throw new InvalidOperationException("Preflight state is not initialized.");
        }

        if (IncludeGraphQlPreflight && PreflightState.GraphQlError is not null)
        {
            preFlightIssues.Add($"GraphQL not reachable at {PreflightState.GraphQlUri}. Error: {PreflightState.GraphQlError}");
        }

        if (PreflightState.WebApiError is not null)
        {
            preFlightIssues.Add($"WebAPI not reachable at {PreflightState.WebApiUri}. Error: {PreflightState.WebApiError}");
        }

        preFlightIssues.Should()
            .BeEmpty($"GraphQL E2E preflight failed:{Environment.NewLine}" +
                     $"{string.Join($"{Environment.NewLine}", preFlightIssues)}");
    }

    protected virtual void ConfigureServices(
        IServiceCollection services,
        E2ESettings settings,
        Uri webApiUri,
        Uri graphQlUri)
    { }

    protected virtual void AfterServiceProviderBuilt(ServiceProvider serviceProvider) { }

    protected virtual bool IncludeGraphQlPreflight => true;

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
        using var request = new HttpRequestMessage(HttpMethod.Head, uri);

        try
        {
            using var _ = await httpClient.SendAsync(request, TestContext.Current.CancellationToken);
            return null;
        }
        catch (Exception exception)
        {
            return exception.GetBaseException().Message;
        }
    }

    private void ClearTokenOverrides() => _tokenOverridesAccessor?.Current = null;
}

public sealed record PreflightState(
    Uri GraphQlUri,
    Uri WebApiUri,
    string? GraphQlError,
    string? WebApiError);
