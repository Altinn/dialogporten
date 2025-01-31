namespace Digdir.Library.Entity.EntityFrameworkCore;

/// <summary>
/// Interface for configuring entity handling behavior options.
/// </summary>
public interface IEntityOptions
{
    /// <summary>
    /// Gets a value indicating whether the soft deletable filter is enabled.
    /// </summary>
    bool EnableSoftDeletableFilter { get; }

    /// <summary>
    /// Gets a value indicating whether the immutable filter is enabled.
    /// </summary>
    bool EnableImmutableFilter { get; }

    /// <summary>
    /// Gets a value indicating whether the versionable filter is enabled.
    /// </summary>
    bool EnableVersionableFilter { get; }

    /// <summary>
    /// Gets a value indicating whether the updatable filter is enabled.
    /// </summary>
    bool EnableUpdatableFilter { get; }

    /// <summary>
    /// Gets a value indicating whether the creatable filter is enabled.
    /// </summary>
    bool EnableCreatableFilter { get; }

    /// <summary>
    /// Gets a value indicating whether the lookup filter is enabled.
    /// </summary>
    bool EnableLookupFilter { get; }

    /// <summary>
    /// Gets a value indicating whether the identifiable filter is enabled.
    /// </summary>
    bool EnableIdentifiableFilter { get; }

    /// <summary>
    /// Gets a value indicating whether the aggregate filter is enabled.
    /// </summary>
    bool EnableAggregateFilter { get; }
}
