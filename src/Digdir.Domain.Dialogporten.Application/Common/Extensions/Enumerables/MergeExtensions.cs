using AutoMapper;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;

internal delegate Task<IEnumerable<TDestination>> CreateAsyncDelegate<TDestination, in TSource>(IEnumerable<TSource> creatables, CancellationToken cancellationToken = default);
internal delegate Task UpdateAsyncDelegate<TDestination, TSource>(IEnumerable<UpdateSet<TDestination, TSource>> updateSets, CancellationToken cancellationToken = default);
internal delegate Task DeleteAsyncDelegate<in TDestination>(IEnumerable<TDestination> deletables, CancellationToken cancellationToken = default);

internal delegate IEnumerable<TDestination> CreateDelegate<out TDestination, in TSource>(IEnumerable<TSource> creatables);
internal delegate void UpdateDelegate<TDestination, TSource>(IEnumerable<UpdateSet<TDestination, TSource>> updateSets);
internal delegate void DeleteDelegate<in TDestination>(IEnumerable<TDestination> deletables);

internal static class DeleteDelegate
{
#pragma warning disable IDE0060
    // ReSharper disable once MethodOverloadWithOptionalParameter
    public static Task Default<TDestination>(IEnumerable<TDestination> deletables, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public static void Default<TDestination>(IEnumerable<TDestination> deletables) { /* No operation by design */ }
#pragma warning restore IDE0060

}

internal static class MergeExtensions
{
    public static void Merge<TDestination, TSource, TKey>(
        this ICollection<TDestination> destinations,
        IEnumerable<TSource> sources,
        Func<TDestination, TKey> destinationKeySelector,
        Func<TSource, TKey> sourceKeySelector,
        CreateDelegate<TDestination, TSource>? create = null,
        UpdateDelegate<TDestination, TSource>? update = null,
        DeleteDelegate<TDestination>? delete = null,
        IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destinations);
        ArgumentNullException.ThrowIfNull(sourceKeySelector);
        ArgumentNullException.ThrowIfNull(destinationKeySelector);
        comparer ??= EqualityComparer<TKey>.Default;
        var concreteSources = sources as List<TSource> ?? sources.ToList();
        var updateSets = UpdateSet<TDestination, TSource>.Create(
            destinations,
            concreteSources,
            destinationKeySelector,
            sourceKeySelector,
            comparer);

        Delete(destinations, delete, updateSets);
        Update(update, updateSets);
        Create(destinations, concreteSources, create, updateSets);
    }

    public static async Task MergeAsync<TDestination, TSource, TKey>(
        this ICollection<TDestination> destinations,
        IEnumerable<TSource> sources,
        Func<TDestination, TKey> destinationKeySelector,
        Func<TSource, TKey> sourceKeySelector,
        CreateAsyncDelegate<TDestination, TSource>? create = null,
        UpdateAsyncDelegate<TDestination, TSource>? update = null,
        DeleteAsyncDelegate<TDestination>? delete = null,
        IEqualityComparer<TKey>? comparer = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(destinations);
        ArgumentNullException.ThrowIfNull(sourceKeySelector);
        ArgumentNullException.ThrowIfNull(destinationKeySelector);
        comparer ??= EqualityComparer<TKey>.Default;
        var concreteSources = sources as List<TSource> ?? sources.ToList();
        var updateSets = UpdateSet<TDestination, TSource>.Create(
            destinations,
            concreteSources,
            destinationKeySelector,
            sourceKeySelector,
            comparer);

        await DeleteAsync(destinations, delete, updateSets, cancellationToken);
        await UpdateAsync(update, updateSets, cancellationToken);
        await CreateAsync(destinations, concreteSources, create, updateSets, cancellationToken);
    }

    internal static void Update<TDestination, TSource>(this IMapperBase mapper, IEnumerable<UpdateSet<TDestination, TSource>> updateSets)
    {
        foreach (var (source, destination) in updateSets)
        {
            mapper.Map(source, destination);
        }
    }

    private static async Task CreateAsync<TDestination, TSource>(
        ICollection<TDestination> destinations,
        IEnumerable<TSource> sources,
        CreateAsyncDelegate<TDestination, TSource>? create,
        List<UpdateSet<TDestination, TSource>> updateSets,
        CancellationToken cancellationToken)
    {
        if (create is null)
            return;

        var creates = sources
            .Except(updateSets.Select(x => x.Source))
            .ToList();

        if (creates.Count == 0)
            return;

        destinations.AddRange(await create(creates, cancellationToken));
    }

    private static async Task UpdateAsync<TDestination, TSource>(
        UpdateAsyncDelegate<TDestination, TSource>? update,
        List<UpdateSet<TDestination, TSource>> updateSets,
        CancellationToken cancellationToken)
    {
        if (update is null || updateSets.Count == 0)
            return;
        await update(updateSets, cancellationToken);
    }

    private static async Task DeleteAsync<TDestination, TSource>(
        ICollection<TDestination> destinations,
        DeleteAsyncDelegate<TDestination>? delete,
        List<UpdateSet<TDestination, TSource>> updateSets,
        CancellationToken cancellationToken)
    {
        if (delete is null)
            return;

        var delegates = destinations
            .Except(updateSets.Select(x => x.Destination))
            .ToList();

        if (delegates.Count == 0)
            return;

        await delete(delegates, cancellationToken);
        foreach (var item in delegates)
        {
            destinations.Remove(item);
        }
    }

    private static void Create<TDestination, TSource>(
        ICollection<TDestination> destinations,
        IEnumerable<TSource> sources,
        CreateDelegate<TDestination, TSource>? create,
        List<UpdateSet<TDestination, TSource>> updateSets)
    {
        if (create is null)
            return;

        var creates = sources
            .Except(updateSets.Select(x => x.Source))
            .ToList();

        if (creates.Count == 0)
            return;

        destinations.AddRange(create(creates));
    }

    private static void Update<TDestination, TSource>(
        UpdateDelegate<TDestination, TSource>? update,
        List<UpdateSet<TDestination, TSource>> updateSets)
    {
        if (update is null || updateSets.Count == 0)
            return;
        update(updateSets);
    }

    private static void Delete<TDestination, TSource>(
        ICollection<TDestination> destinations,
        DeleteDelegate<TDestination>? delete,
        List<UpdateSet<TDestination, TSource>> updateSets)
    {
        if (delete is null)
            return;

        var delegates = destinations
            .Except(updateSets.Select(x => x.Destination))
            .ToList();

        if (delegates.Count == 0)
            return;

        delete(delegates);
        foreach (var item in delegates)
        {
            destinations.Remove(item);
        }
    }

    private static void AddRange<T>(
        this ICollection<T> destination,
        IEnumerable<T> source)
    {
        if (destination is List<T> list)
        {
            list.AddRange(source);
            return;
        }

        foreach (var item in source)
        {
            destination.Add(item);
        }
    }
}
