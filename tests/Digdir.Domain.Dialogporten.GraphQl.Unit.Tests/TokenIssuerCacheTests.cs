using Digdir.Domain.Dialogporten.GraphQL.Common;
using Digdir.Domain.Dialogporten.GraphQL.Common.Authentication;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests;

public class TokenIssuerCacheTests
{
    [Fact]
    public async Task EnsureInitializedAsync_Should_Not_Deadlock_When_Called_Concurrently()
    {
        // Arrange
        var settings = new GraphQlSettings
        {
            Authentication = new AuthenticationOptions
            {
                JwtBearerTokenSchemas = []
            },
            Cors = new GraphQlCorsOptions { AllowedOrigins = ["*"] }
        };
        var cache = new TokenIssuerCache(Options.Create(settings));

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => cache.GetIssuerForScheme("test"));

        // Act
        var allTasks = Task.WhenAll(tasks);
        var completed = await Task.WhenAny(allTasks, Task.Delay(TimeSpan.FromSeconds(1)));

        // Assert
        Assert.Same(allTasks, completed);
        await allTasks; // propagate exceptions if any

        cache.Dispose();
    }
}
