using Digdir.Domain.Dialogporten.Domain.Common;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions;

internal static class ReadOnlyCollectionExtensions
{
    private const int Cycle = int.MaxValue;

    /// <summary>
    /// Validates the reference hierarchy in a collection of entities, checking for depth, cyclic references, and width violations.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entities in the collection.</typeparam>
    /// <typeparam name="TKey">The type of the key used to identify entities.</typeparam>
    /// <param name="entities">The collection of entities to validate.</param>
    /// <param name="keySelector">A function to select the key for each entity.</param>
    /// <param name="parentKeySelector">A function to select the parent key for each entity.</param>
    /// <param name="propertyName">The name of the property being validated.</param>
    /// <param name="maxDepth">The maximum allowed depth of the hierarchy. Default is 100.</param>
    /// <param name="maxWidth">The maximum allowed width of the hierarchy. Default is 1.</param>
    /// <returns>A list of <see cref="DomainFailure"/> objects representing any validation errors found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if an entity's parent key is not found in the collection.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an entity's <paramref name="keySelector"/> returns default <typeparamref name="TKey"/>.</exception>
    public static List<DomainFailure> ValidateReferenceHierarchy<TEntity, TKey>(
        this IReadOnlyCollection<TEntity> entities,
        Func<TEntity, TKey> keySelector,
        Func<TEntity, TKey?> parentKeySelector,
        string propertyName,
        int maxDepth = 100,
        int maxWidth = 1)
        where TKey : struct
    {
        if (entities.Count == 0)
        {
            return [];
        }

        entities.Select(keySelector).EnsureNonDefaultTKey();

        var maxDepthViolation = maxDepth + 1;
        var type = typeof(TEntity);
        var errors = new List<DomainFailure>();

        var invalidReferences = GetInvalidReferences(entities, keySelector, parentKeySelector);
        if (invalidReferences.Count > 0)
        {
            var ids = $"[{string.Join(",", invalidReferences)}]";
            errors.Add(new DomainFailure(propertyName,
                $"Hierarchy reference violation found. " +
                $"{type.Name} with the following referenced ids does not exist: {ids}."));

            return errors;
        }

        var depthByKey = entities
            .ToDictionary(keySelector)
            .ToDepthByKey(keySelector, parentKeySelector);

        var depthErrors = depthByKey
            .Where(x => x.Value == maxDepthViolation)
            .ToList();

        var cycleErrors = depthByKey
            .Where(x => x.Value == Cycle)
            .ToList();

        var widthErrors = entities
            .Where(x => parentKeySelector(x) is not null)
            .GroupBy(parentKeySelector)
            .Where(x => x.Count() > maxWidth)
            .ToList();

        if (depthErrors.Count > 0)
        {
            var ids = $"[{string.Join(",", depthErrors.Select(x => x.Key))}]";
            errors.Add(new DomainFailure(propertyName,
                $"Hierarchy depth violation found. {type.Name} with the following " +
                $"ids is at depth {maxDepthViolation}, exceeding the max allowed depth of {maxDepth}. " +
                $"It, and all its referencing children is in violation of the depth constraint. {ids}."));
        }

        if (cycleErrors.Count > 0)
        {
            var firstTenFailedIds = cycleErrors.Take(10).Select(x => x.Key).ToList();
            var cycleCutOffInfo = cycleErrors.Count > 10 ? " (showing first 10)" : string.Empty;

            var joinedIds = $"[{string.Join(",", firstTenFailedIds)}]";
            errors.Add(new DomainFailure(propertyName,
                $"Hierarchy cyclic reference violation found. {type.Name} with the " +
                $"following ids is part of a cyclic reference chain{cycleCutOffInfo}: {joinedIds}."));
        }

        if (widthErrors.Count > 0)
        {
            var ids = $"[{string.Join(",", widthErrors.Select(x => x.Key))}]";
            errors.Add(new DomainFailure(propertyName,
                $"Hierarchy width violation found. '{type.Name}' with the following " +
                $"ids has to many referring {type.Name}, exceeding the max " +
                $"allowed width of {maxWidth}: {ids}."));
        }

        return errors;
    }

    private static List<TKey> GetInvalidReferences<TEntity, TKey>(IReadOnlyCollection<TEntity> entities,
        Func<TEntity, TKey> keySelector,
        Func<TEntity, TKey?> parentKeySelector) where TKey : struct => entities
        .Where(x => parentKeySelector(x).HasValue)
        .Select(x => parentKeySelector(x)!.Value)
        .Except(entities.Select(keySelector))
        .ToList();

    private static Dictionary<TKey, int> ToDepthByKey<TKey, TEntity>(
        this Dictionary<TKey, TEntity> transmissionById,
        Func<TEntity, TKey> keySelector,
        Func<TEntity, TKey?> parentKeySelector)
        where TKey : struct
    {
        var depthByKey = new Dictionary<TKey, int>();
        var breadCrumbs = new HashSet<TKey>();
        foreach (var (_, current) in transmissionById)
        {
            GetDepth(current, transmissionById, keySelector, parentKeySelector, depthByKey, breadCrumbs);
        }

        return depthByKey;
    }

    private static int GetDepth<TEntity, TKey>(TEntity current,
        Dictionary<TKey, TEntity> entitiesById,
        Func<TEntity, TKey> keySelector,
        Func<TEntity, TKey?> parentKeySelector,
        Dictionary<TKey, int> cachedDepthByVisited,
        HashSet<TKey> breadCrumbs)
        where TKey : struct
    {
        var key = keySelector(current);
        if (breadCrumbs.Contains(key))
        {
            return Cycle;
        }

        if (cachedDepthByVisited.TryGetValue(key, out var cachedDepth))
        {
            return cachedDepth;
        }

        breadCrumbs.Add(key);
        var parentKey = parentKeySelector(current);
        var parentDepth = !parentKey.HasValue ? 0
            : entitiesById.TryGetValue(parentKey.Value, out var parent)
                ? GetDepth(parent, entitiesById, keySelector, parentKeySelector, cachedDepthByVisited, breadCrumbs)
                : throw new InvalidOperationException(
                    $"{nameof(entitiesById)} does not contain expected " +
                    $"key '{parentKey.Value}'.");

        breadCrumbs.Remove(key);
        return cachedDepthByVisited[key] = parentDepth == Cycle ? Cycle : ++parentDepth;
    }

    private static void EnsureNonDefaultTKey<TKey>(this IEnumerable<TKey> keys) where TKey : struct
    {
        if (keys.Any(key => EqualityComparer<TKey>.Default.Equals(key, default)))
        {
            throw new InvalidOperationException("All keys must be non-default.");
        }
    }
}
