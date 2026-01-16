using System.Text.Json;
using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.Features.V1;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
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

    private PreflightState? PreflightState { get; set; }

    public IDialogportenGraphQlTestClient GraphQlClient { get; private set; } = null!;
    public IServiceownerApi ServiceownerApi { get; private set; } = null!;

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

    public void PreflightCheck()
    {
        var issues = new List<string>();

        if (PreflightState is null)
        {
            throw new InvalidOperationException("Preflight state is not initialized.");
        }

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
            throw SkipException.ForSkip(
                $"GraphQL E2E preflight failed:{Environment.NewLine}{string.Join($"{Environment.NewLine}", issues)}");
        }
    }

    public void CleanupAfterTest()
    {
        var overridesAccessor = _tokenOverridesAccessor ?? new TokenOverridesAccessor();
        _tokenOverridesAccessor ??= overridesAccessor;

        overridesAccessor.Current = new TokenOverrides(
            ServiceOwner: new ServiceOwnerTokenOverrides(
                Scopes: TestTokenConstants.ServiceOwnerScopes + " " + AuthorizationScope.ServiceOwnerAdminScope));

        var cancellationToken = TestContext.Current.CancellationToken;
        var queryParams = new V1ServiceOwnerDialogsQueriesSearchDialogQueryParams
        {
            ServiceResource = ["urn:altinn:resource:ttd-dialogporten-automated-tests"],
            Limit = 1000
        };

        PaginatedListOfV1ServiceOwnerDialogsQueriesSearch_Dialog? page;
        do
        {
            var searchResult = ServiceownerApi
                .V1ServiceOwnerDialogsQueriesSearchDialog(queryParams, cancellationToken)
                .GetAwaiter()
                .GetResult();

            if (!searchResult.IsSuccessful || searchResult.Content is null)
            {
                TestContext.Current?.AddWarning(
                    $"Failed to search dialogs for cleanup: {searchResult.Error?.Message ?? "unknown error"}");
                TestContext.Current?.AddWarning($"{searchResult.Error?.Message ?? "unknown error"}");
                return;
            }

            page = searchResult.Content;
            foreach (var dialog in page.Items ?? [])
            {
                try
                {
                    var purgeResult = ServiceownerApi
                        .V1ServiceOwnerDialogsCommandsPurgeDialog(dialog.Id, if_Match: null, cancellationToken)
                        .GetAwaiter()
                        .GetResult();

                    if (!purgeResult.IsSuccessful)
                    {
                        TestContext.Current?.AddWarning(
                            $"Failed to delete dialog {dialog.Id}: {purgeResult.Error?.Message ?? "unknown error"}");
                    }
                }
                catch (Exception exception)
                {
                    TestContext.Current?.AddWarning(
                        $"Failed to delete dialog {dialog.Id}: {exception.GetBaseException().Message}");
                }
            }

            if (page.HasNextPage)
            {
                queryParams.ContinuationToken = new()
                {
                    AdditionalProperties = new Dictionary<string, object>
                    {
                        ["continuationToken"] = page.ContinuationToken
                    }
                };
            }
        } while (page.HasNextPage);
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
        using var request = new HttpRequestMessage(HttpMethod.Head, uri);

        try
        {
            using var _ = await httpClient.SendAsync(request);
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
