using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Labels;

public class BulkSetSystemLabel
{
    /// <summary>
    /// List of target dialog ids with optional revision ids
    /// </summary>
    [JsonPropertyName("dialogs")]
    public ICollection<DialogRevision>? Dialogs { get; set; }

    /// <summary>
    /// List of system labels to set on target dialogs
    /// </summary>
    [JsonPropertyName("systemLabels")]
    [Obsolete("Use AddLabels instead. This property will be removed in a future version.")]
    public ICollection<SystemLabel> SystemLabels { get; set; } = [];

    /// <summary>
    /// List of system labels to add to the target dialogs. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.
    /// </summary>
    [JsonPropertyName("addLabels")]
    public ICollection<SystemLabel> AddLabels { get; set; } = [];

    /// <summary>
    /// List of system labels to remove from the target dialogs. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.
    /// </summary>
    [JsonPropertyName("removeLabels")]
    public ICollection<SystemLabel> RemoveLabels { get; set; } = [];

    /// <summary>
    /// Optional actor metadata describing who performed the operation. Only available for admin-integrations when EndUserId is omitted.
    /// </summary>
    [JsonPropertyName("performedBy")]
    public Actor? PerformedBy { get; set; }
}

public class SetDialogSystemLabelRequest
{
    /// <summary>
    /// List of system labels to set on target dialogs
    /// </summary>
    [JsonPropertyName("systemLabels")]
    [Obsolete("Use AddLabels instead. This property will be removed in a future version.")]
    public ICollection<SystemLabel> SystemLabels { get; set; } = [];

    /// <summary>
    /// List of system labels to add to target dialogs. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.
    /// </summary>
    [JsonPropertyName("addLabels")]
    public ICollection<SystemLabel> AddLabels { get; set; } = [];

    /// <summary>
    /// List of system labels to remove from target dialogs. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.
    /// </summary>
    [JsonPropertyName("removeLabels")]
    public ICollection<SystemLabel> RemoveLabels { get; set; } = [];

    /// <summary>
    /// Optional actor metadata describing who performed the change. Only available for admin-integrations when EnduserId is omitted.
    /// </summary>
    [JsonPropertyName("performedBy")]
    public Actor? PerformedBy { get; set; }
}
