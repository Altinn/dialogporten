namespace Digdir.Library.Entity.Abstractions.Features.Versionable;

/// <summary>
/// Abstraction implemented by entities to keep track of their version.
/// </summary>
public interface IVersionableEntity
{
    /// <summary>
    /// The entity tag unique to each version of the entity.
    /// </summary>
    Guid Revision { get; set; }
}
