using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.FeatureMetric;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

public class FeatureMetricMiddlewareTests
{
    [Theory]
    [InlineData(Constants.VersionEndpointPaths.ServiceOwner)]
    [InlineData(Constants.VersionEndpointPaths.EndUser)]
    public async Task Should_Not_Report_Version_Endpoint_Requests(string path)
    {
        var deliveryContext = new RecordingDeliveryContext();

        await InvokeMiddleware(path, deliveryContext);

        deliveryContext.PresentationTags.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Report_Other_Requests()
    {
        var deliveryContext = new RecordingDeliveryContext();

        await InvokeMiddleware("/api/v1/serviceowner/dialogs", deliveryContext);

        deliveryContext.PresentationTags.Should().ContainSingle();
    }

    private static async Task InvokeMiddleware(string path, IFeatureMetricDeliveryContext deliveryContext)
    {
        await using var services = new ServiceCollection()
            .AddSingleton(deliveryContext)
            .BuildServiceProvider();
        var middleware = new FeatureMetricMiddleware(_ => Task.CompletedTask, Options.Create(new FeatureMetricOptions()));
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;

        await middleware.InvokeAsync(context);
    }

    private sealed class RecordingDeliveryContext : IFeatureMetricDeliveryContext
    {
        public List<string> PresentationTags { get; } = [];

        public void ReportOutcome(string presentationTag, params IEnumerable<KeyValuePair<string, object>> additionalTags) =>
            PresentationTags.Add(presentationTag);
    }
}
