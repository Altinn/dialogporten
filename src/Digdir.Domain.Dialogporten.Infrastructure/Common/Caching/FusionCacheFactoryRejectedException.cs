namespace Digdir.Domain.Dialogporten.Infrastructure.Common.Caching;

/// <summary>
/// Thrown by <see cref="FusionCacheFactoryRunner"/> when a cache factory invocation is rejected because the
/// cache's execution permits are exhausted (its factories are stuck or abandoned up to the configured
/// concurrency bound). Cache callers with fail-safe data are served stale; cold misses observe this exception.
/// </summary>
public sealed class FusionCacheFactoryRejectedException : Exception
{
    public FusionCacheFactoryRejectedException()
    {
    }

    public FusionCacheFactoryRejectedException(string message) : base(message)
    {
    }

    public FusionCacheFactoryRejectedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
