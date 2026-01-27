namespace Digdir.Library.Entity.Abstractions.Features.SoftDeletable;

/// <summary>
/// Provides extension methods for <see cref="ISoftDeletableEntity"/>.
/// </summary>
public static class SoftDeletableExtensions
{
    /// <param name="deletable">The <see cref="ISoftDeletableEntity"/> to soft delete.</param>
    extension(ISoftDeletableEntity deletable)
    {
        /// <summary>
        /// Marks a <see cref="ISoftDeletableEntity"/> as soft deleted.
        /// </summary>
        /// <param name="now">The deletion time in UTC.</param>
        public void SoftDelete(DateTimeOffset now)
        {
            deletable.DeletedAt = now;
            deletable.Deleted = true;
        }

        /// <summary>
        /// Restores a <see cref="ISoftDeletableEntity"/>.
        /// </summary>
        public void Restore()
        {
            deletable.DeletedAt = null;
            deletable.Deleted = false;
        }
    }
}
