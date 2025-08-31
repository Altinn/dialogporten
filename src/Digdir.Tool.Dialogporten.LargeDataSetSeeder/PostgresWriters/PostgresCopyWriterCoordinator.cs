using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal class PostgresCopyWriterCoordinator
{
    private readonly NpgsqlDataSource _dataSource;
    // ReSharper disable once NotAccessedField.Local
#pragma warning disable IDE0052
    private readonly int _maxConnections;
#pragma warning restore IDE0052

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
        var poolAwaiters = new List<PoolWriter>();
        var connectionPerPool = 1; // _maxConnections / EntityGeneratorExtensions.Generators.Count;
        var rest = 0; // _maxConnections % EntityGeneratorExtensions.Generators.Count;

        foreach (var (targetType, generator) in EntityGeneratorExtensions.Generators)
        {
            var entityEnumerable = generator(DialogTimestamp.Generate(fromDate, toDate, dialogAmount));
            var writer = await PostgresCopyWriterPoolFactory.Create(targetType, _dataSource, initialConsumers: connectionPerPool + (rest-- > 0 ? 1 : 0));
            var poolWriterTask = Task.Run(async () => await writer.WriteAsync(entityEnumerable));
            poolAwaiters.Add(new PoolWriter(poolWriterTask, writer));
        }

        await foreach (var poolAwaiterTask in Task.WhenEach(poolAwaiters.Select(x => x.WaitAndReturnPool()).ToList()))
        {
            var poolAwaiter = await poolAwaiterTask;
            await poolAwaiter.Pool.DisposeAsync();
            poolAwaiters.Remove(poolAwaiter);

            // // redistribute disposed connections
            // connectionPerPool = _maxConnections / EntityGeneratorExtensions.Generators.Count;
            // rest = _maxConnections % EntityGeneratorExtensions.Generators.Count;
            // foreach (var pool in poolAwaiters.Select(x => x.Pool))
            // {
            //     await pool.ScaleTo(connectionPerPool + (rest-- > 0 ? 1 : 0));
            // }
        }
    }

    private sealed class PoolWriter(Task poolWriterTask, IPostgresCopyWriterPool pool)
    {
        public IPostgresCopyWriterPool Pool => pool;

        public async Task<PoolWriter> WaitAndReturnPool()
        {
            await poolWriterTask;
            return this;
        }
    }
}


