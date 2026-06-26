using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Activity;

public class DialogActivitySearchItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("extendedType")]
    public Uri? ExtendedType { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<DialogActivityType>))]
    public DialogActivityType Type { get; set; }

    [JsonPropertyName("transmissionId")]
    public Guid? TransmissionId { get; set; }

    [JsonPropertyName("description")]
    public ICollection<Localization> Description { get; set; } = [];
    public enum DialogTransmissionType
    {
        [System.Runtime.Serialization.EnumMember(Value = @"Information")]
        Information = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"Acceptance")]
        Acceptance = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"Rejection")]
        Rejection = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"Request")]
        Request = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"Alert")]
        Alert = 4,

        [System.Runtime.Serialization.EnumMember(Value = @"Decision")]
        Decision = 5,

        [System.Runtime.Serialization.EnumMember(Value = @"Submission")]
        Submission = 6,

        [System.Runtime.Serialization.EnumMember(Value = @"Correction")]
        Correction = 7,
    }

}
