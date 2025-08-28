using Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal class PostgresCopyWriterCoordinator
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly int _maxConnections;

    public PostgresCopyWriterCoordinator(
        NpgsqlDataSource dataSource,
        int maxConnections = 75)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConnections);
        _maxConnections = maxConnections;
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task Handle(
        DateTimeOffset fromDate,
        DateTimeOffset toDate,
        int dialogAmount)
    {
        var poolAwaiters = new List<PoolAwaiter>();
        var connectionPerPool = _maxConnections / EntityGeneratorExtensions.Generators.Count;

        foreach (var (targetType, generator) in EntityGeneratorExtensions.Generators)
        {
            var entityEnumerable = generator(DialogTimestamp.Generate(fromDate, toDate, dialogAmount));
            var writer = await PostgresCopyWriterPoolFactory.Create(targetType, _dataSource, initialConsumers: connectionPerPool);
            var task = writer.WriteAsync(entityEnumerable);
            poolAwaiters.Add(new PoolAwaiter(task, writer));
        }

        await foreach (var poolAwaiterTask in Task.WhenEach(poolAwaiters.Select(x => x.WaitAndReturnPool()).ToList()))
        {
            var poolAwaiter = await poolAwaiterTask;
            await poolAwaiter.Pool.DisposeAsync();
            // poolAwaiters.Remove(poolAwaiter);

            // // redistribute disposed connections
            // connectionPerPool = _maxConnextions / EntityGeneratorExtensions.Generators.Count;
            // foreach (var pool in poolAwaiters.Select(x => x.Pool))
            // {
            //     await pool.ScaleUp(connectionPerPool - pool.ConsumerCount);
            // }
        }
    }

    private sealed class PoolAwaiter(Task task, IPostgresCopyWriterPool pool)
    {
        public IPostgresCopyWriterPool Pool => pool;

        public async Task<PoolAwaiter> WaitAndReturnPool()
        {
            await task;
            return this;
        }
    }
}


