using System.Net;
using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.ServiceOwner;
using Altinn.ApiClients.Maskinporten.Config;
using Altinn.ApiClients.Maskinporten.Extensions;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Models;
using Altinn.ApiClients.Maskinporten.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Digdir.Library.Dialogporten.WebApiClient.Unit.Tests;

public class AddDialogportenClientTests
{
    private const string MaskinportenClientKey = "test-maskinporten-client";
    private const string ExpectedAccessToken = "test-access-token";

    [Fact]
    public void CustomAuthentication_InvokesDelegatePerRefitClient_WithoutWiringMaskinporten()
    {
        // Arrange
        var services = new ServiceCollection();
        var settings = new DialogportenSettings { BaseUri = "https://example.com" };
        var configuredClientNames = new List<string>();

        // Act
        services.AddDialogportenClient(settings, builder => configuredClientNames.Add(builder.Name));

        // Assert
        // The ServiceOwner package exposes two Refit clients (IServiceownerApi + IMetadataApi),
        // so the authentication delegate must be invoked once for each of them.
        Assert.Equal(2, configuredClientNames.Count);

        // No Maskinporten settings were required, and the SDK did not register a Maskinporten client definition itself.
        Assert.Null(settings.Maskinporten);
        var provider = services.BuildServiceProvider();
        Assert.Empty(provider.GetServices<IClientDefinition>());
    }

    [Fact]
    public async Task CustomAuthentication_AllowsConsumerToAttachMaskinportenFromNuget()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Fake the Maskinporten token service so the test never calls Maskinporten over the network.
        // AddMaskinportenHttpMessageHandler registers the real service via TryAdd, so our fake wins.
        var maskinportenService = Substitute.For<IMaskinportenService>();
        maskinportenService
            .GetToken(Arg.Any<IClientDefinition>(), Arg.Any<MaskinportenRequestContext>(), Arg.Any<bool>())
            .Returns(new TokenResponse { AccessToken = ExpectedAccessToken });
        services.AddSingleton(maskinportenService);

        // Consumer registers their own Maskinporten client definition, exactly as a real consumer would.
        var maskinportenSettings = new MaskinportenSettings
        {
            ClientId = "client-id",
            Scope = "digdir:dialogporten.serviceprovider",
            Environment = "test"
        };
        services.RegisterMaskinportenClientDefinition<SettingsJwkClientDefinition>(MaskinportenClientKey, maskinportenSettings);

        var capturingHandler = new CapturingHandler();
        var settings = new DialogportenSettings { BaseUri = "https://example.com" };
        string? clientName = null;

        // Act
        services.AddDialogportenClient(settings, builder =>
        {
            clientName ??= builder.Name;
            builder.AddMaskinportenHttpMessageHandler<SettingsJwkClientDefinition>(MaskinportenClientKey);
            builder.ConfigurePrimaryHttpMessageHandler(() => capturingHandler);
        });

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient(clientName!);
        await client.GetAsync("https://example.com/api/v1/serviceowner/dialogs", TestContext.Current.CancellationToken);

        // Assert
        // The consumer-attached Maskinporten handler ran and set the Authorization header.
        Assert.NotNull(capturingHandler.LastRequest);
        Assert.Equal("Bearer", capturingHandler.LastRequest!.Headers.Authorization?.Scheme);
        Assert.Equal(ExpectedAccessToken, capturingHandler.LastRequest.Headers.Authorization?.Parameter);
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
