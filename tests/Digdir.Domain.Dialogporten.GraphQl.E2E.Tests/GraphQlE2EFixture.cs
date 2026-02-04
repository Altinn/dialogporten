using Digdir.Library.Dialogporten.E2E.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests;

public sealed class GraphQlE2EFixture : E2EFixtureBase
{
    public IDialogportenGraphQlTestClient GraphQlClient { get; private set; } = null!;

    protected override void ConfigureServices(
        IServiceCollection services,
        E2ESettings settings,
        Uri webApiUri,
        Uri graphQlUri)
    {
        services
            .AddDialogportenGraphQlTestClient()
            .ConfigureHttpClient(
                httpClient => httpClient.BaseAddress = graphQlUri,
                builder => builder.AddHttpMessageHandler(serviceProvider =>
                    ActivatorUtilities.CreateInstance<TestTokenHandler>(serviceProvider, TokenKind.EndUser)));
    }

    protected override void AfterServiceProviderBuilt(ServiceProvider serviceProvider) =>
        GraphQlClient = serviceProvider.GetRequiredService<IDialogportenGraphQlTestClient>();
}
