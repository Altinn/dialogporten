namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;

/// <summary>
/// A stub standing in for an element the authenticated user is not authorized for, and whose authorization
/// context asks for it to be excluded rather than disabled. The element is removed from the collection it
/// belongs to and recorded here instead, so that a client can tell "this existed and you cannot see it"
/// apart from "this does not exist" without being shown anything about it.
/// </summary>
public sealed class ExcludedElementDto
{
    /// <summary>
    /// The identifier of the excluded element, matching the id it would have had in its own collection.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The UTC timestamp when the excluded element was created. Lets a client order exclusions against the
    /// elements it can see, e.g. to tell where in a transmission thread something is missing.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
