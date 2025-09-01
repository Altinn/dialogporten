using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;
using Npgsql;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal class PostgresCopyWriterCoordinator
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ITypeDistributor _typeDistributor;
    private readonly int _maxConnections;

    public PostgresCopyWriterCoordinator(
        NpgsqlDataSource dataSource,
        ITypeDistributor typeDistributor,
        int maxConnections = 75)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConnections);
        _maxConnections = maxConnections;
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _typeDistributor = typeDistributor ?? throw new ArgumentNullException(nameof(typeDistributor));
    }

    public async Task Handle(DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
    {
        var poolAwaiters = await CreatePoolAwaiters(fromDate, toDate, dialogAmount);
        await foreach (var poolAwaiterTask in Task.WhenEach(poolAwaiters.Select(x => x.WaitAndReturnPool()).ToList()))
        {
            var poolAwaiter = await poolAwaiterTask;
            await poolAwaiter.Pool.DisposeAsync();
            poolAwaiters.Remove(poolAwaiter);
            await RedistributeConnections(poolAwaiters);
        }
    }

    private async Task<List<PoolWriter>> CreatePoolAwaiters(DateTimeOffset fromDate, DateTimeOffset toDate, int dialogAmount)
    {
        var poolAwaiters = new List<PoolWriter>();
        var scalingByType = _typeDistributor.GetDistribution(_maxConnections, EntityGeneratorExtensions.Generators.Keys);
        foreach (var (targetType, generator) in EntityGeneratorExtensions.Generators)
        {
            var entityEnumerable = generator(DialogTimestamp.Generate(fromDate, toDate, dialogAmount));
            var writer = await PostgresCopyWriterPoolFactory.Create(targetType, _dataSource, scalingByType[targetType]);
            var poolWriterTask = Task.Run(async () => await writer.WriteAsync(entityEnumerable));
            poolAwaiters.Add(new PoolWriter(poolWriterTask, writer));
        }

        return poolAwaiters;
    }

    private async Task RedistributeConnections(List<PoolWriter> poolAwaiters)
    {
        var scalingByType = _typeDistributor.GetDistribution(_maxConnections, EntityGeneratorExtensions.Generators.Keys);
        foreach (var pool in poolAwaiters.Select(x => x.Pool))
        {
            await pool.ScaleTo(scalingByType[pool.Type]);
        }
    }

    private static Dictionary<Type, int> GetScalingByType(int maxConnections, IEnumerable<Type> types)
    {
        var typeList = types.ToArray();
        var result = new Dictionary<Type, int>();
        var count = typeList.Length;
        var baseShare = maxConnections / count;
        var remainder = maxConnections % count;

        for (var i = 0; i < count; i++)
        {
            var allocation = baseShare + (i < remainder ? 1 : 0);
            result[typeList[i]] = allocation;
        }

        return result;
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


