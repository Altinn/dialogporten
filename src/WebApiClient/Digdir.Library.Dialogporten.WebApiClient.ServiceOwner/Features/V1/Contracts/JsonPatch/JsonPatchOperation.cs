using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.JsonPatch;

public enum JsonPatchOperationType
{
    [System.Runtime.Serialization.EnumMember(Value = @"Add")]
    Add = 0,

    [System.Runtime.Serialization.EnumMember(Value = @"Remove")]
    Remove = 1,

    [System.Runtime.Serialization.EnumMember(Value = @"Replace")]
    Replace = 2,

    [System.Runtime.Serialization.EnumMember(Value = @"Move")]
    Move = 3,

    [System.Runtime.Serialization.EnumMember(Value = @"Copy")]
    Copy = 4,

    [System.Runtime.Serialization.EnumMember(Value = @"Test")]
    Test = 5,

    [System.Runtime.Serialization.EnumMember(Value = @"Invalid")]
    Invalid = 6,
}

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
