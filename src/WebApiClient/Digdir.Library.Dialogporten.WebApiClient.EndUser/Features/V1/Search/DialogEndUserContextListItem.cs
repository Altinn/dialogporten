using System.Text.Json.Serialization;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Enums;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

public class DialogEndUserContextListItem
{
    /// <summary>
    /// The unique identifier for the end user context revision in UUIDv4 format.
    /// </summary>
    [JsonPropertyName("revision")]
    public Guid Revision { get; set; }

    /// <summary>
    /// System defined labels used to categorize dialogs.
    /// </summary>
    [JsonPropertyName("systemLabels")]
    // TODO(system.text.json): Add ItemConverterType with enum converter when supported
    public ICollection<SystemLabel>? SystemLabels { get; set; } = [];
}
