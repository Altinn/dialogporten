using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal class PostgresCopyWriterCoordinator
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IWorkerPoolBalancer _workerPoolBalancer;

    public PostgresCopyWriterCoordinator(
        NpgsqlDataSource dataSource,
        IWorkerPoolBalancer workerPoolBalancer)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _workerPoolBalancer = workerPoolBalancer ?? throw new ArgumentNullException(nameof(workerPoolBalancer));
    }

    public async Task Handle(DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
    {
        var poolAwaiters = await CreatePoolAwaiters(fromDate, toDate, dialogAmount);
        await foreach (var poolAwaiterTask in Task.WhenEach(poolAwaiters.Select(x => x.WaitAndReturnPool())))
        {
            var poolAwaiter = await poolAwaiterTask;
            await poolAwaiter.Pool.DisposeAsync();
            poolAwaiters.Remove(poolAwaiter);
            await RedistributeConnections(poolAwaiters);
        }
    }

    private async Task<List<PoolWriter>> CreatePoolAwaiters(DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
    {
        var poolWriterArray = await Task.WhenAll(EntityGeneratorExtensions.Generators
            .Select(x => Task.Run(async () =>
            {
                var (targetType, generator) = x;
                var entityEnumerable = generator(DialogTimestamp.Generate(fromDate, toDate, dialogAmount));
                var writer = await PostgresCopyWriterPoolFactory.Create(targetType, _dataSource);
                var poolWriterTask = Task.Run(async () => await writer.WriteAsync(entityEnumerable));
                return new PoolWriter(poolWriterTask, writer);
            })));
        var poolWriters = poolWriterArray.ToList();
        await RedistributeConnections(poolWriters);
        return poolWriterArray.ToList();
    }

    private async Task RedistributeConnections(List<PoolWriter> poolAwaiters)
    {
        var scalingByType = _workerPoolBalancer
            .CalculateBalanceByType(poolAwaiters.Select(x => x.Pool));
        await Task.WhenAll(poolAwaiters
            .Select(x => x.Pool)
            .Select(x => x.ScaleTo(scalingByType[x])));
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


