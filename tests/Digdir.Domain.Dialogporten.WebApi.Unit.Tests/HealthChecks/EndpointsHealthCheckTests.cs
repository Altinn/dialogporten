using Digdir.Library.Utils.AspNet;
using Digdir.Library.Utils.AspNet.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.HealthChecks;

public class EndpointsHealthCheckTests
{
    [Fact]
    public void ResolveHttpGetEndpointToCheck_Should_Resolve_AltinnPlatformRelativePath_Against_BaseUri()
    {
        var resolved = AspNetUtilitiesExtensions.ResolveHttpGetEndpointToCheck(
            new HttpGetEndpointToCheck
            {
                Name = "Altinn authorization",
                AltinnPlatformRelativePath = "authorization/health",
                HardDependency = true
            },
            new Uri("https://platform.altinn.no/"));

        Assert.Equal("https://platform.altinn.no/authorization/health", resolved.Url);
        Assert.Null(resolved.AltinnPlatformRelativePath);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Return_Healthy_When_All_Endpoints_Are_Healthy()
    {
        var sut = CreateHealthCheck(
            [CreateEndpoint("Auth", "https://auth.example.test", hardDependency: true)],
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Equal("All endpoints are healthy.", result.Description);
        Assert.Equal(1, result.Data["totalCount"]);
        Assert.Equal(0, result.Data["hardFailureCount"]);
        Assert.Equal(0, result.Data["softFailureCount"]);
        Assert.Equal(0, result.Data["slowCount"]);
        Assert.Single((IEnumerable<object?>)result.Data["checkedEndpoints"]);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Return_Degraded_When_Soft_Dependency_Fails()
    {
        var sut = CreateHealthCheck(
            [CreateEndpoint("Optional dependency", "https://optional.example.test", hardDependency: false)],
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Degraded endpoints", result.Description);
        Assert.Contains("Optional dependency", result.Description);
        Assert.Equal(1, result.Data["softFailureCount"]);
        Assert.Equal(0, result.Data["hardFailureCount"]);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Return_Unhealthy_When_Hard_Dependency_Times_Out()
    {
        var sut = CreateHealthCheck(
            [CreateEndpoint("Critical dependency", "https://critical.example.test", hardDependency: true)],
            _ => throw new OperationCanceledException("Simulated timeout"));

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("Hard dependency failures", result.Description);
        Assert.Contains("Critical dependency", result.Description);
        Assert.Equal(1, result.Data["hardFailureCount"]);
        Assert.Equal(0, result.Data["softFailureCount"]);
    }

    [Fact]
    public async Task CheckHealthAsync_Should_Return_Degraded_When_Hard_Dependency_Is_Slow()
    {
        var sut = CreateHealthCheck(
            [CreateEndpoint("Critical dependency", "https://critical.example.test", hardDependency: true)],
            async cancellationToken =>
            {
                await Task.Delay(TimeSpan.FromSeconds(6), cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var result = await sut.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Degraded endpoints", result.Description);
        Assert.Contains("responded slowly", result.Description);
        Assert.Equal(1, result.Data["slowCount"]);
        Assert.Equal(0, result.Data["hardFailureCount"]);
    }

    private static EndpointsHealthCheck CreateHealthCheck(
        List<HttpGetEndpointToCheck> endpoints,
        Func<CancellationToken, Task<HttpResponseMessage>> sendAsync) =>
        new(
            new FakeHttpClientFactory(sendAsync),
            NullLogger<EndpointsHealthCheck>.Instance,
            Options.Create(new AspNetUtilitiesSettings
            {
                HealthCheckSettings = new HealthCheckSettings
                {
                    HttpGetEndpointsToCheck = endpoints
                }
            }));

    private static HttpGetEndpointToCheck CreateEndpoint(string name, string url, bool hardDependency) =>
        new()
        {
            Name = name,
            Url = url,
            HardDependency = hardDependency
        };

    private sealed class FakeHttpClientFactory(Func<CancellationToken, Task<HttpResponseMessage>> sendAsync) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHttpMessageHandler(sendAsync));
    }

    private sealed class StubHttpMessageHandler(Func<CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            sendAsync(cancellationToken);
    }
}
