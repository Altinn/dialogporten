using System.Net;
using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using Azure.Core.Pipeline;
using Digdir.Library.Utils.AspNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public class AzureAppConfigurationExtensionsTests
{
    private const string ConnectionStringKey = "Infrastructure:DialogDbConnectionString";
    private const string AuthModeKey = "Infrastructure:DialogDbAuth:Mode";
    private const string PasswordFreeConnectionString = "Host=fixture.postgres.database.azure.com;Database=dialogporten;SSL Mode=VerifyFull";
    private const string AdministratorConnectionString = "Host=fixture.postgres.database.azure.com;Database=dialogporten;Username=admin;Password=fixture";

    [Fact]
    public async Task EntraToken_Should_Only_Resolve_Other_Secrets_On_Load_And_Refresh()
    {
        using var configuration = CreateBootstrapConfiguration("EntraToken", PasswordFreeConnectionString);
        using var handler = new AppConfigurationHandler();
        using var client = new HttpClient(handler);
        var resolvedSecrets = new List<string>();
        var refresher = AddAzureProvider(configuration, client, handler, resolvedSecrets);

        configuration[ConnectionStringKey].Should().Be(PasswordFreeConnectionString);
        configuration["RedisConnectionString"].Should().Be("redis-1");
        resolvedSecrets.Should().Equal("/secrets/redis/1");

        // Rotate the referenced secret version so refresh resolves a new URI without waiting for
        // the provider's minimum one-minute cache lifetime for an unchanged secret reference.
        handler.Revision = 2;
        await Task.Delay(TimeSpan.FromMilliseconds(1100), TestContext.Current.CancellationToken);
        await refresher.RefreshAsync(TestContext.Current.CancellationToken);

        configuration[ConnectionStringKey].Should().Be(PasswordFreeConnectionString);
        configuration["RedisConnectionString"].Should().Be("redis-2");
        configuration["Sentinel"].Should().Be("2");
        resolvedSecrets.Should().Equal("/secrets/redis/1", "/secrets/redis/2");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Password")]
    public void Password_Mode_Should_Continue_Resolving_The_Database_Secret(string? mode)
    {
        using var configuration = CreateBootstrapConfiguration(mode, null);
        using var handler = new AppConfigurationHandler();
        using var client = new HttpClient(handler);
        var resolvedSecrets = new List<string>();

        AddAzureProvider(configuration, client, handler, resolvedSecrets);

        configuration[ConnectionStringKey].Should().Be(AdministratorConnectionString);
        configuration["RedisConnectionString"].Should().Be("redis-1");
        resolvedSecrets.Should().BeEquivalentTo("/secrets/database", "/secrets/redis/1");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(AdministratorConnectionString)]
    [InlineData(PasswordFreeConnectionString + ";Passfile=/tmp/fixture.pgpass")]
    public void EntraToken_Should_Reject_Missing_Or_Password_Bearing_Bootstrap_Connection_Strings(string? connectionString)
    {
        using var configuration = CreateBootstrapConfiguration("EntraToken", connectionString);
        var options = new AzureAppConfigurationOptions();

        var act = () => options.ConfigureDialogDatabaseConnection(configuration);

        act.Should().Throw<InvalidOperationException>();
    }

    private static ConfigurationManager CreateBootstrapConfiguration(string? mode, string? connectionString)
    {
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [AuthModeKey] = mode,
            [ConnectionStringKey] = connectionString
        });
        return configuration;
    }

    private static IConfigurationRefresher AddAzureProvider(
        ConfigurationManager configuration,
        HttpClient client,
        AppConfigurationHandler handler,
        List<string> resolvedSecrets)
    {
        IConfigurationRefresher? refresher = null;
        configuration.AddAzureAppConfiguration(options =>
        {
            options.ReplicaDiscoveryEnabled = false;
            options.Connect("Endpoint=https://fixture.azconfig.io;Id=fixture;Secret=ZmFrZQ==")
                .ConfigureClientOptions(clientOptions => clientOptions.Transport = new HttpClientTransport(client))
                .ConfigureDialogDatabaseConnection(configuration)
                .ConfigureRefresh(refresh => refresh.Register("Sentinel", refreshAll: true)
                    .SetRefreshInterval(TimeSpan.FromSeconds(1)))
                .ConfigureKeyVault(keyVault => keyVault
                    .SetSecretRefreshInterval(TimeSpan.FromMinutes(1))
                    .SetSecretResolver(uri =>
                    {
                        resolvedSecrets.Add(uri.AbsolutePath);
                        return ValueTask.FromResult(uri.AbsolutePath == "/secrets/database"
                            ? AdministratorConnectionString
                            : $"redis-{handler.Revision}");
                    }));
            refresher = options.GetRefresher();
        });

        refresher.Should().NotBeNull();
        return refresher;
    }

    private sealed class AppConfigurationHandler : HttpMessageHandler
    {
        public int Revision { get; set; } = 1;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var sentinel = new
            {
                key = "Sentinel",
                value = Revision.ToString(System.Globalization.CultureInfo.InvariantCulture),
                etag = $"revision-{Revision}"
            };
            object[] items =
            [
                sentinel,
                SecretReference(ConnectionStringKey, "database"),
                SecretReference("RedisConnectionString", $"redis/{Revision}")
            ];
            var json = request.RequestUri?.AbsolutePath == "/kv/Sentinel"
                ? JsonSerializer.Serialize(sentinel)
                : JsonSerializer.Serialize(new { items });
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            response.Headers.ETag = new($"\"revision-{Revision}\"");
            return Task.FromResult(response);
        }

        private static object SecretReference(string key, string name) => new
        {
            key,
            value = JsonSerializer.Serialize(new { uri = $"https://fixture.vault.azure.net/secrets/{name}" }),
            content_type = "application/vnd.microsoft.appconfig.keyvaultref+json;charset=utf-8",
            etag = $"secret-reference-{name}"
        };
    }
}
