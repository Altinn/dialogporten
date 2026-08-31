using System.Text.Json.Serialization;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Common;

/// <summary>
/// A stub standing in for an element the authenticated user is not authorized for, and whose authorization
/// <br/>context asks for it to be excluded rather than disabled. The element is removed from the collection it
/// <br/>belongs to and recorded here instead, so that a client can tell "this existed and you cannot see it"
/// <br/>apart from "this does not exist" without being shown anything about it.
/// </summary>
public class ExcludedElement
{
    /// <summary>
    /// The identifier of the excluded element, matching the id it would have had in its own collection.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The UTC timestamp when the excluded element was created. Lets a client order exclusions against the
    /// <br/>elements it can see, e.g. to tell where in a transmission thread something is missing.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }
}
