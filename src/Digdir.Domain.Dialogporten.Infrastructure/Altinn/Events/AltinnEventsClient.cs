using System.Net.Http.Headers;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Common.Serialization;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Events;

internal sealed class AltinnEventsClient : ICloudEventBus
{
    private readonly HttpClient _client;

    public AltinnEventsClient(HttpClient client)
    {
        _client = client;
    }

    public async Task Publish(CloudEvent cloudEvent, CancellationToken cancellationToken)
        => await _client.PostAsJsonEnsuredAsync(
            "/events/api/v1/events",
            cloudEvent,
            serializerOptions: SerializerOptions.CloudEventSerializerOptions,
            configureContentHeaders: h
                => h.ContentType = new MediaTypeHeaderValue("application/cloudevents+json"),
            cancellationToken: cancellationToken);
}

internal sealed partial class ConsoleLogEventBus : ICloudEventBus
{
    // Used by source-generated logging partials; analyzers don't see the generated usage.
#pragma warning disable IDE0052
    private readonly ILogger<ConsoleLogEventBus> _logger;
#pragma warning restore IDE0052

    public ConsoleLogEventBus(ILogger<ConsoleLogEventBus> logger)
    {
        _logger = logger;
    }

    public Task Publish(CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        LogEventPublished(cloudEvent.Time, cloudEvent.Type);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Event published! Time: {CloudEventTime:O}, Type: {CloudEventType}")]
    private partial void LogEventPublished(DateTimeOffset? cloudEventTime, string? cloudEventType);
}
