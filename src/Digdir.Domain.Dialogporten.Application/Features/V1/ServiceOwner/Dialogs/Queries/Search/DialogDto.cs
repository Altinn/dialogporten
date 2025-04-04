using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;

public sealed class DialogDto : DialogDtoBase
{
    /// <summary>
    /// The content of the dialog in search results.
    /// </summary>
    [JsonPropertyOrder(100)] // ILU MAGNUS
    public ContentDto? Content { get; set; }
}

public sealed class ContentDto
{
    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public ContentValueDto Title { get; set; } = null!;

    /// <summary>
    /// An optional non-sensitive title of the dialog.
    /// Used for search and list views if the user authorization does not meet the required eIDAS level
    /// </summary>
    public ContentValueDto? NonSensitiveTitle { get; set; }

    /// <summary>
    /// A short summary of the dialog and its current state.
    /// </summary>
    public ContentValueDto Summary { get; set; } = null!;

    /// <summary>
    /// An optional non-sensitive summary of the dialog and its current state.
    /// Used for search and list views if the user authorization does not meet the required eIDAS level
    /// </summary>
    public ContentValueDto? NonSensitiveSummary { get; set; }

    /// <summary>
    /// Overridden sender name. If not supplied, assume "org" as the sender name.
    /// </summary>
    public ContentValueDto? SenderName { get; set; }

    /// <summary>
    /// Used as the human-readable label used to describe the "ExtendedStatus" field.
    /// </summary>
    public ContentValueDto? ExtendedStatus { get; set; }
}

/// <summary>
/// TODO: Discuss this with the team later. It works for now
/// This class is used to keep using ProjectTo and existing PaginationList code.
/// We first map to this using ProjectTo, then map to the new DialogContent structure
/// in the SearchDialog handlers, after EF core is done loading the data.
/// Then we create a new PaginatedList with the outwards facing dto.
/// </summary>
public sealed class IntermediateDialogDto : DialogDtoBase
{
    public List<DialogContent> Content { get; set; } = [];
}
