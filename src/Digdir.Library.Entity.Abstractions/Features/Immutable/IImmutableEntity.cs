using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Library.Entity.Abstractions.Features.Immutable;

/// <summary>
/// Convenience interface to mark an entity as immutable.
/// See Digdir.Library.Entity.EntityFrameworkCore.Features.Immutable.HandleImmutableEntities.
/// </summary>
public interface IImmutableEntity :
    IIdentifiableEntity;
