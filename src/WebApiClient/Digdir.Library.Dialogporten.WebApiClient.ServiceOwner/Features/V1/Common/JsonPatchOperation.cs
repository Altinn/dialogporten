using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;

public class JsonPatchOperation
{
    [JsonPropertyName("operationType")]
    [JsonConverter(typeof(JsonStringEnumConverter<JsonPatchOperationType>))]
    public JsonPatchOperationType OperationType { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("op")]
    public string? Op { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }
}
