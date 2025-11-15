namespace Digdir.Library.Entity.Abstractions.Features.Updatable;

/// <summary>
/// Provides extension methods for <see cref="IUpdateableEntity"/>.
/// </summary>
public static class UpdatableExtensions
{
    /// <summary>
    /// Sets properties related to <see cref="IUpdateableEntity"/>.
    /// </summary>
    /// <param name="updateable">The <see cref="IUpdateableEntity"/> to update.</param>
    /// <param name="utcNow">The update time in UTC.</param>
    public static void Update(this IUpdateableEntity updateable, DateTimeOffset utcNow)
    {
        // Aggregates sometimes set UpdatedAt explicitly (e.g. to VisibleFrom). Only replace the value when time moves forward so
        // those overrides are preserved while keeping UpdatedAt monotonic.
        if (utcNow > updateable.UpdatedAt)
        {
            updateable.UpdatedAt = utcNow;
        }
    }
}
