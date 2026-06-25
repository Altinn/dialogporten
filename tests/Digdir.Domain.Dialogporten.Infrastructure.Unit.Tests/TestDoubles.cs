using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

// Shared test doubles reused across the service-resource / authorization unit tests, so the same fakes are not
// copy-pasted per test class.

internal static class TestFusionCache
{
    /// <summary>
    /// An <see cref="IFusionCacheProvider"/> backed by a single in-process <see cref="FusionCache"/> for the
    /// given cache name (L1-only, no serializer/backplane) — enough to exercise GetOrSet caching behavior.
    /// </summary>
    public static IFusionCacheProvider CreateProvider(string cacheName) =>
        new StubFusionCacheProvider(new FusionCache(Options.Create(new FusionCacheOptions { CacheName = cacheName })));
}

internal sealed class StubFusionCacheProvider(IFusionCache cache) : IFusionCacheProvider
{
    public IFusionCache GetCache(string cacheName) => cache;
    public IFusionCache? GetCacheOrNull(string cacheName) => cache;
}

internal sealed class StubUser(ClaimsPrincipal principal) : IUser
{
    public ClaimsPrincipal GetPrincipal() => principal;
}

internal sealed class StubOptionsSnapshot<T>(T value) : IOptionsSnapshot<T> where T : class
{
    public T Value => value;
    public T Get(string? name) => value;
}
