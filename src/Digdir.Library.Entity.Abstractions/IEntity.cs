using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Library.Entity.Abstractions.Features.Updatable;

namespace Digdir.Library.Entity.Abstractions;

/// <summary>
/// Convenience interface to mark an entity with
/// <see cref="IIdentifiableEntity"/>,
/// <see cref="ICreatableEntity"/>, and
/// <see cref="IUpdateableEntity"/>.
/// </summary>
public interface IEntity :
    IIdentifiableEntity,
    ICreatableEntity,
    IUpdateableEntity;
