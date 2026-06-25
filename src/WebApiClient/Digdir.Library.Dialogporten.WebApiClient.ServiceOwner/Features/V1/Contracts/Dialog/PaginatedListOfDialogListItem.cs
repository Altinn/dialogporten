using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Contracts.Dialog;

public class PaginatedListOfDialogListItem
{
    /// <summary>
    /// The paginated list of items
    /// </summary>
    [JsonPropertyName("items")]
    public ICollection<DialogListItem>? Items { get; set; }

    /// <summary>
    /// Whether there are more items available that can be fetched by supplying the continuation token
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    /// <summary>
    /// The continuation token to be used to fetch the next page of items
    /// </summary>
    [JsonPropertyName("continuationToken")]
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// The current sorting order of the items
    /// </summary>
    [JsonPropertyName("orderBy")]
    public string OrderBy { get; set; } = null!;
}