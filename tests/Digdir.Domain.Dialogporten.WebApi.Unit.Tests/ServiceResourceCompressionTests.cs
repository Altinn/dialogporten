using System.IO.Compression;
using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Library.Utils.AspNet;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests;

/// <summary>
/// Exercises the WebApi opt-in pipeline end-to-end: the attribute-scanning helper
/// (<see cref="ResponseCompressionEndpointFilterExtensions"/>) attaches an endpoint filter to a
/// decorated endpoint, and the globally registered ResponseCompression middleware sees the resulting
/// <c>IHttpsCompressionFeature.Mode = Compress</c> at body-write time.
/// </summary>
public class ServiceResourceCompressionTests
{
    private const string CompressedPath = "/compressed";
    private const string PlainPath = "/plain";

    private static readonly string Payload = "{\"items\":[" + string.Join(",",
        Enumerable.Range(0, 200).Select(i =>
            $"{{\"id\":{i},\"name\":\"resource-{i}\",\"desc\":\"lorem ipsum dolor sit amet consectetur\"}}"))
        + "]}";

    private static TestServer CreateServer()
    {
        var builder = new HostBuilder().ConfigureWebHost(webHost => webHost
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddDialogportenResponseCompression();
            })
            .Configure(app =>
            {
                // ResponseCompression only honors IHttpsCompressionFeature.Mode on HTTPS requests
                // (EnableForHttps is false). TestServer defaults to http; spoof the scheme so tests
                // exercise the production opt-in behavior.
                app.Use((context, next) =>
                {
                    context.Request.Scheme = "https";
                    return next();
                });
                app.UseResponseCompression();
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet(CompressedPath, () => Results.Content(Payload, "application/json"))
                        .AddResponseCompressionHintIfMarked(typeof(MarkedHandler));

                    endpoints.MapGet(PlainPath, () => Results.Content(Payload, "application/json"))
                        .AddResponseCompressionHintIfMarked(typeof(UnmarkedHandler));
                });
            }));
        return builder.Start().GetTestServer();
    }

    [Theory]
    [InlineData("br, gzip", "br")]
    [InlineData("br", "br")]
    [InlineData("gzip", "gzip")]
    public async Task Compresses_response_when_endpoint_is_marked_and_AcceptEncoding_matches(
        string acceptEncoding, string expectedEncoding)
    {
        using var server = CreateServer();
        using var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, CompressedPath);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", acceptEncoding);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.Should().ContainSingle().Which.Should().Be(expectedEncoding);

        var raw = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        raw.Length.Should().BeLessThan(Payload.Length);

        using Stream decompressed = expectedEncoding == "br"
            ? new BrotliStream(new MemoryStream(raw), CompressionMode.Decompress)
            : new GZipStream(new MemoryStream(raw), CompressionMode.Decompress);
        using var reader = new StreamReader(decompressed);
        var decoded = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        decoded.Should().Be(Payload);
    }

    [Fact]
    public async Task Does_not_compress_when_no_AcceptEncoding_header()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();

        var response = await client.GetAsync(CompressedPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.Should().BeEmpty();
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be(Payload);
    }

    [Fact]
    public async Task Does_not_compress_when_endpoint_type_is_unmarked()
    {
        using var server = CreateServer();
        using var client = server.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, PlainPath);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "br, gzip");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentEncoding.Should().BeEmpty();
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Be(Payload);
    }

    // Test-only stand-ins for FastEndpoints endpoint classes; only their reflected method-level
    // attribute matters to ResponseCompressionEndpointFilterExtensions.
#pragma warning disable CA1822 // Instance methods so the helper's BindingFlags.Instance scan finds them.
    private sealed class MarkedHandler
    {
        [EnableResponseCompression]
        public void HandleAsync() { }
    }

    private sealed class UnmarkedHandler
    {
        public void HandleAsync() { }
    }
#pragma warning restore CA1822
}
