using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using static System.Text.Json.Serialization.JsonIgnoreCondition;

namespace Digdir.Library.Dialogporten.E2E.Common;

public abstract class E2EFixtureBase : IAsyncLifetime
{
    private ServiceProvider? _serviceProvider;
    private ITokenOverridesAccessor? _tokenOverridesAccessor;

    private PreflightState? PreflightState { get; set; }
    public string DotnetEnvironment { get; private set; } = Environments.Development;
    public E2ESettings Settings { get; private set; } = null!;

    /// <summary>
    /// The WebAPI base URI, for tests that need to reach unauthenticated endpoints outside the generated
    /// clients (e.g. the JWKS document used to verify dialog token signatures).
    /// </summary>
    public Uri WebApiUri { get; private set; } = null!;

    public IServiceownerApi ServiceownerApi { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        DotnetEnvironment = Environment.GetDotnetEnvironment();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{DotnetEnvironment}.json", optional: true)
            .AddJsonFile("appsettings.local.json", optional: true)
            .AddUserSecrets<E2ESettings>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        ThrowIfRemovedSettingsArePresent(configuration);

        var settings = configuration.Get<E2ESettings>()
                       ?? throw new InvalidOperationException("E2E settings are missing.");
        ThrowIfServiceUrisAreMissing(settings);
        Settings = settings;

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

        // Refit appends its routes (which start with '/') to the base address, so it must not end with '/'.
        // Raw HttpClients resolve relative paths against WebApiUri, so that one must end with '/'.
        var refitBaseAddress = WithTrailingSlash(settings.WebApiBaseUri, trailingSlash: false);
        WebApiUri = WithTrailingSlash(settings.WebApiBaseUri, trailingSlash: true);

        services
            .AddRefitClient<IServiceownerApi>(new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
            })
            .ConfigureHttpClient(httpClient =>
            {
                httpClient.BaseAddress = refitBaseAddress;
            })
            .AddHttpMessageHandler(serviceProvider =>
                ActivatorUtilities.CreateInstance<TestTokenHandler>(serviceProvider, TokenKind.ServiceOwner));

        services.Decorate<IServiceownerApi, EphemeralDialogDecorator>();

        var graphQlUri = settings.GraphQlUri;

        ConfigureServices(services, settings, refitBaseAddress, graphQlUri);

        _serviceProvider = services.BuildServiceProvider();

        ServiceownerApi = _serviceProvider.GetRequiredService<IServiceownerApi>();
        _tokenOverridesAccessor = _serviceProvider.GetRequiredService<ITokenOverridesAccessor>();

        AfterServiceProviderBuilt(_serviceProvider);

        PreflightState = await CreatePreflightState(graphQlUri, refitBaseAddress);
    }

    public ValueTask DisposeAsync()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    public IHttpClientFactory GetHttpClientFactory() => _serviceProvider!.GetRequiredService<IHttpClientFactory>();

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

    public IDisposable UseSystemUserTokenOverrides(
        Func<TokenGeneratorEnvironment, string>? systemUserId = null,
        string? systemUserOrg = null,
        string? scopes = null,
        string? tokenOverride = null) =>
        UseTokenOverrides(new TokenOverrides(
            EndUserType: EndUserTokenType.SystemUser,
            SystemUser: new SystemUserTokenOverrides(systemUserId, systemUserOrg, scopes, tokenOverride)));

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

    private static readonly (string RemovedKey, string Replacement)[] RemovedSettings =
    [
        ("DialogportenBaseUri", nameof(E2ESettings.WebApiBaseUri)),
        ("WebApiPort", nameof(E2ESettings.WebApiBaseUri)),
        ("GraphQlPort", nameof(E2ESettings.GraphQlUri))
    ];

    private static void ThrowIfRemovedSettingsArePresent(IConfigurationRoot configuration)
    {
        var issues = RemovedSettings
            .Select(removed => (removed.RemovedKey, removed.Replacement, Provider: configuration.Providers
                .LastOrDefault(provider => provider.TryGet(removed.RemovedKey, out _))))
            .Where(removed => removed.Provider is not null)
            .Select(removed =>
                $"'{removed.RemovedKey}' is no longer supported; set '{removed.Replacement}' to the full URL instead. " +
                $"It was set by {removed.Provider}.")
            .ToList();

        if (issues.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"E2E settings contain removed keys (check appsettings.local.json, user secrets and environment variables):" +
            $"{Environment.NewLine}{string.Join(Environment.NewLine, issues)}");
    }

    private static void ThrowIfServiceUrisAreMissing(E2ESettings settings)
    {
        string[] issues =
        [
            .. MissingAbsoluteUri(nameof(E2ESettings.WebApiBaseUri), settings.WebApiBaseUri),
            .. MissingAbsoluteUri(nameof(E2ESettings.GraphQlUri), settings.GraphQlUri)
        ];

        if (issues.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"E2E settings are invalid:{Environment.NewLine}{string.Join(Environment.NewLine, issues)}");
    }

    private static IEnumerable<string> MissingAbsoluteUri(string key, Uri? uri) => uri switch
    {
        null => [$"'{key}' is missing."],
        { IsAbsoluteUri: false } => [$"'{key}' must be an absolute URL, got '{uri}'."],
        _ => []
    };

    private static Uri WithTrailingSlash(Uri uri, bool trailingSlash)
    {
        var path = uri.AbsolutePath.TrimEnd('/');
        return new UriBuilder(uri) { Path = trailingSlash ? path + "/" : path }.Uri;
    }

    private void ClearTokenOverrides() => _tokenOverridesAccessor?.Current = null;
}

public sealed record PreflightState(
    Uri GraphQlUri,
    Uri WebApiUri,
    string? GraphQlError,
    string? WebApiError);
