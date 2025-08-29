using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal static class PostgresCopyWriterPoolFactory
{
    [SuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    public static async Task<IPostgresCopyWriterPool> Create(
        Type entityType,
        NpgsqlDataSource dataSource,
        int initialConsumers = 1,
        int capacity = 10_000)
    {
        ArgumentNullException.ThrowIfNull(entityType);
        ArgumentNullException.ThrowIfNull(dataSource);

        // Construct generic type: PostgresCopyWriterPool<entityType>
        var poolType = typeof(PostgresCopyWriterPool<>).MakeGenericType(entityType);

        // Get static Create method
        var createMethod = poolType.GetMethod(
                               nameof(PostgresCopyWriterPool<object>.Create),
                               BindingFlags.Public | BindingFlags.Static)
                           ?? throw new InvalidOperationException($"Create method not found on {poolType}");

        // Invoke Create<T>(dataSource, initialConsumers, capacity)
        var task = (Task)createMethod.Invoke(null, [dataSource, initialConsumers, capacity])!;
        await task.ConfigureAwait(false);
        // Extract Result via reflection
        return (IPostgresCopyWriterPool)task.GetType()
            .GetProperty(nameof(Task<object>.Result))!
            .GetValue(task)!;
    }
}
