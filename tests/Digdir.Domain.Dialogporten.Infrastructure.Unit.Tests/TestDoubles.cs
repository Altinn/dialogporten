using System.Collections.Concurrent;
using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

// Shared test doubles reused across the service-resource / authorization unit tests, so the same fakes are not
// copy-pasted per test class.

internal static class TestFusionCache
{
    /// <summary>
    /// An <see cref="IFusionCacheProvider"/> backed by a single in-process <see cref="FusionCache"/> for the
    /// given cache name (L1-only, no serializer/backplane) — enough to exercise GetOrSet caching behavior. The
    /// provider only serves this one cache name, so a component asking for the wrong named cache fails the test.
    /// </summary>
    public static IFusionCacheProvider CreateProvider(string cacheName) =>
        new StubFusionCacheProvider(cacheName, new FusionCache(Options.Create(new FusionCacheOptions { CacheName = cacheName })));
}

internal sealed class StubFusionCacheProvider(string cacheName, IFusionCache cache) : IFusionCacheProvider
{
    public IFusionCache GetCache(string name) =>
        name == cacheName
            ? cache
            : throw new InvalidOperationException(
                $"Requested cache '{name}' but this provider only serves '{cacheName}'.");

    public IFusionCache? GetCacheOrNull(string name) => name == cacheName ? cache : null;
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

/// <summary>
/// Captures log entries for assertions. Thread-safe: cache factories detached by the runner log concurrently
/// with the test's own thread.
/// </summary>
internal sealed class CollectingLogger<T> : ILogger<T>
{
    private readonly ConcurrentQueue<CollectedLogEntry> _entries = new();

    public IReadOnlyCollection<CollectedLogEntry> Entries => _entries;

    IDisposable? ILogger.BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _entries.Enqueue(new CollectedLogEntry(logLevel, eventId, exception, formatter(state, exception)));
}

internal sealed record CollectedLogEntry(LogLevel Level, EventId EventId, Exception? Exception, string Message);
