using AwesomeAssertions;
using Digdir.Domain.Dialogporten.GraphQL.Common;
using Digdir.Library.Utils.AspNet;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests;

public class EnableResponseCompressionInterceptorTests
{
    [Fact]
    public async Task Decorated_resolver_sets_HttpsCompression_mode_to_Compress()
    {
        var (executor, feature) = await BuildExecutorAsync();

        var result = await executor.ExecuteAsync("{ marked }", TestContext.Current.CancellationToken);

        result.ExpectOperationResult().Errors.Should().BeNullOrEmpty();
        feature.Mode.Should().Be(HttpsCompressionMode.Compress);
    }

    [Fact]
    public async Task Undecorated_resolver_leaves_HttpsCompression_mode_at_default()
    {
        var (executor, feature) = await BuildExecutorAsync();

        var result = await executor.ExecuteAsync("{ plain }", TestContext.Current.CancellationToken);

        result.ExpectOperationResult().Errors.Should().BeNullOrEmpty();
        feature.Mode.Should().Be(HttpsCompressionMode.Default);
    }

    [Fact]
    public async Task Decorated_mutation_does_not_set_HttpsCompression_mode()
    {
        var (executor, feature) = await BuildExecutorAsync(withMutation: true);

        var result = await executor.ExecuteAsync("mutation { markedMutation }", TestContext.Current.CancellationToken);

        result.ExpectOperationResult().Errors.Should().BeNullOrEmpty();
        feature.Mode.Should().Be(HttpsCompressionMode.Default);
    }

    private static async Task<(IRequestExecutor Executor, FakeHttpsCompressionFeature Feature)> BuildExecutorAsync(
        bool withMutation = false)
    {
        var httpContext = new DefaultHttpContext();
        var feature = new FakeHttpsCompressionFeature();
        httpContext.Features.Set<IHttpsCompressionFeature>(feature);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpContextAccessor>(new StubHttpContextAccessor(httpContext));
        var builder = services
            .AddGraphQLServer()
            .AddQueryType<TestQuery>()
            .TryAddTypeInterceptor<EnableResponseCompressionTypeInterceptor>();
        if (withMutation)
        {
            builder.AddMutationType<TestMutation>();
        }

        var provider = services.BuildServiceProvider();
        var executor = await provider.GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        return (executor, feature);
    }

    private sealed class FakeHttpsCompressionFeature : IHttpsCompressionFeature
    {
        public HttpsCompressionMode Mode { get; set; } = HttpsCompressionMode.Default;
    }

    private sealed class StubHttpContextAccessor(HttpContext httpContext) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = httpContext;
    }
}

public sealed class TestQuery
{
#pragma warning disable CA1822 // Resolver methods must be instance members so HotChocolate binds them as fields.
    [EnableResponseCompression]
    public string Marked() => "ok";

    public string Plain() => "ok";
#pragma warning restore CA1822
}

public sealed class TestMutation
{
#pragma warning disable CA1822
    [EnableResponseCompression]
    public string MarkedMutation() => "ok";
#pragma warning restore CA1822
}
