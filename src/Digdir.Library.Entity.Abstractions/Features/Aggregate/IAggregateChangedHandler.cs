namespace Digdir.Library.Entity.Abstractions.Features.Aggregate;

/// <summary>
/// Defines an entity handler for managing its own aggregate, including creation, updating, and deletion.
/// </summary>
public interface IAggregateChangedHandler :
    IAggregateCreatedHandler,
    IAggregateUpdatedHandler,
    IAggregateDeletedHandler;

/// <summary>
/// Defines an entity handler for handling the creation of its own aggregate.
/// </summary>
public interface IAggregateCreatedHandler
{
    /// <summary>
    /// Handles the creation of the entity, including associated metadata.
    /// </summary>
    /// <param name="self">The aggregate representing this entity with metadata.</param>
    /// <param name="utcNow">The timestamp of the creation event in UTC time.</param>
    void OnCreate(AggregateNode self, DateTimeOffset utcNow);
}

/// <summary>
/// Defines an entity handler for handling updates to its own aggregate.
/// </summary>
public interface IAggregateUpdatedHandler
{
    /// <summary>
    /// Handles updates to the entity, including associated metadata.
    /// </summary>
    /// <param name="self">The aggregate representing this entity with metadata.</param>
    /// <param name="utcNow">The timestamp of the update event in UTC time.</param>
    /// <param name="enableUpdatableFilter">Indicates whether the updatable filter is enabled for this update.</param>
    void OnUpdate(AggregateNode self, DateTimeOffset utcNow, bool enableUpdatableFilter = false);
}

/// <summary>
/// Defines an entity handler for handling the deletion of its own aggregate.
/// </summary>
public interface IAggregateDeletedHandler
{
    /// <summary>
    /// Handles the deletion of the entity, including associated metadata.
    /// </summary>
    /// <param name="self">The aggregate representing this entity with metadata.</param>
    /// <param name="utcNow">The timestamp of the deletion event in UTC time.</param>
    void OnDelete(AggregateNode self, DateTimeOffset utcNow);
}

/// <summary>
/// Defines an entity handler for handling the deletion of its own aggregate.
/// </summary>
public interface IAggregateRestoredHandler
{
    /// <summary>
    /// Handles the restoration of the entity, including associated metadata.
    /// </summary>
    /// <param name="self">The aggregate representing this entity with metadata.</param>
    /// <param name="utcNow">The timestamp of the restoration event in UTC time.</param>
    void OnRestore(AggregateNode self, DateTimeOffset utcNow);
}
