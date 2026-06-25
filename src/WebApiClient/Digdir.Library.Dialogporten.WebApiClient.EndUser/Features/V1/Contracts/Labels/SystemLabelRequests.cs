using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts.Labels;

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
    public ICollection<SystemLabel>? SystemLabels { get; set; }

    /// <summary>
    /// List of system labels to add to the target dialogs. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.
    /// </summary>
    [JsonPropertyName("addLabels")]
    public ICollection<SystemLabel>? AddLabels { get; set; }

    /// <summary>
    /// List of system labels to remove from the target dialogs. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.
    /// </summary>
    [JsonPropertyName("removeLabels")]
    public ICollection<SystemLabel>? RemoveLabels { get; set; }
}

public class SetDialogSystemLabelRequest
{
    /// <summary>
    /// List of system labels to set on target dialogs
    /// </summary>
    [JsonPropertyName("systemLabels")]
    [Obsolete("Use AddLabels instead. This property will be removed in a future version.")]
    public ICollection<SystemLabel>? SystemLabels { get; set; }

    /// <summary>
    /// List of system labels to add to the target dialog. If multiple instances of 'bin', 'archive', or 'default' are provided, the last one will be used.
    /// </summary>
    [JsonPropertyName("addLabels")]
    public ICollection<SystemLabel>? AddLabels { get; set; }

    /// <summary>
    /// List of system labels to remove from the target dialog. If 'bin' or 'archive' is removed, the 'default' label will be added automatically unless 'bin' or 'archive' is also in the AddLabels list.
    /// </summary>
    [JsonPropertyName("removeLabels")]
    public ICollection<SystemLabel>? RemoveLabels { get; set; }
}

public class DialogRevision
{
    /// <summary>
    /// Target dialog id for system labels
    /// </summary>
    [JsonPropertyName("dialogId")]
    public Guid DialogId { get; set; }

    /// <summary>
    /// Optional end user context revision to match against. If supplied and not matching current revision, the entire operation will fail.
    /// </summary>
    [JsonPropertyName("endUserContextRevision")]
    public Guid? EndUserContextRevision { get; set; }
}
