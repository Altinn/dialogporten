using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence;

internal static class ChangeTrackerExtensions
{
    extension(DbContext db)
    {
        /// <summary>
        /// Disables <see cref="ChangeTracker.AutoDetectChangesEnabled"/> until the returned scope is disposed,
        /// at which point the previous value is restored.
        /// </summary>
        public AutoDetectChangesScope DisableAutoDetectChanges()
        {
            var changeTracker = db.ChangeTracker;
            var scope = new AutoDetectChangesScope(changeTracker, changeTracker.AutoDetectChangesEnabled);
            changeTracker.AutoDetectChangesEnabled = false;
            return scope;
        }
    }

    internal readonly struct AutoDetectChangesScope(ChangeTracker changeTracker, bool previousValue) : IDisposable
    {
        public void Dispose() => changeTracker.AutoDetectChangesEnabled = previousValue;
    }
}
