using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.SystemLabels;

public partial class SetDialogSystemLabelRequest
{
    /// <summary>
    /// List of system labels to set on target dialogs
    /// </summary>
    [JsonPropertyName("systemLabels")]
    // TODO(system.text.json): Add ItemConverterType with enum converter when supported
    [Obsolete("Use AddLabels instead. This property will be removed in a future version.")]
    public ICollection<SystemLabel>? SystemLabels { get; set; }

    /// <summary>
    /// List of system labels to add to target dialogs. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.
    /// </summary>
    [JsonPropertyName("addLabels")]
    // TODO(system.text.json): Add ItemConverterType with enum converter when supported
    public ICollection<SystemLabel>? AddLabels { get; set; }

    /// <summary>
    /// List of system labels to remove from target dialogs. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.
    /// </summary>
    [JsonPropertyName("removeLabels")]
    // TODO(system.text.json): Add ItemConverterType with enum converter when supported
    public ICollection<SystemLabel>? RemoveLabels { get; set; }

    /// <summary>
    /// Optional actor metadata describing who performed the change. Only available for admin-integrations when EnduserId is omitted.
    /// </summary>
    [JsonPropertyName("performedBy")]
    public Actor? PerformedBy { get; set; }
}
