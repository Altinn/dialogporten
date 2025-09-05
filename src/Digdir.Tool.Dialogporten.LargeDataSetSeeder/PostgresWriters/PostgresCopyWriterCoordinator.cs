using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal class PostgresCopyWriterCoordinator
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ITypeDistributor _typeDistributor;

    public PostgresCopyWriterCoordinator(
        NpgsqlDataSource dataSource,
        ITypeDistributor typeDistributor)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _typeDistributor = typeDistributor ?? throw new ArgumentNullException(nameof(typeDistributor));
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
        var scalingByType = _typeDistributor.GetDistribution(EntityGeneratorExtensions.Generators.Keys);
        var poolWriters = await Task.WhenAll(EntityGeneratorExtensions.Generators
            .Select(x => Task.Run(async () =>
            {
                var (targetType, generator) = x;
                var entityEnumerable = generator(DialogTimestamp.Generate(fromDate, toDate, dialogAmount));
                var writer = await PostgresCopyWriterPoolFactory.Create(targetType, _dataSource, scalingByType[targetType]);
                var poolWriterTask = Task.Run(async () => await writer.WriteAsync(entityEnumerable));
                return new PoolWriter(poolWriterTask, writer);
            })));
        return poolWriters.ToList();
    }

    private async Task RedistributeConnections(List<PoolWriter> poolAwaiters)
    {
        var scalingByType = _typeDistributor
            .GetDistribution(poolAwaiters.Select(x => x.Pool.Type));
        await Task.WhenAll(poolAwaiters
            .Select(x => x.Pool)
            .Select(x => x.ScaleTo(scalingByType[x.Type])));
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


