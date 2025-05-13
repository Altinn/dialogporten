using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Updatable;

namespace Digdir.Library.Entity.Abstractions.Features.Immutable;

/// <summary>
/// Convenience interface to mark an entity with
/// <see cref="IImmutableEntity"/>, and
/// <see cref="ICreatableEntity"/>.
/// <remarks>
/// Differs from <see cref="IEntity"/> by not
/// including <see cref="IUpdateableEntity"/>.
/// </remarks>
/// </summary>

public interface IImmutableTimestampedEntity :
    IImmutableEntity,
    ICreatableEntity;

