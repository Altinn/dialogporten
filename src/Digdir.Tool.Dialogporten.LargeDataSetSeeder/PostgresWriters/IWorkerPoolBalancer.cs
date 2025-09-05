namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal interface IWorkerPool
{
    Type Type { get; }
    int WorkerCount { get; }
    int WorkerLoad { get; }
}

internal interface IWorkerPoolBalancer
{
    Dictionary<IWorkerPool, int> CalculateBalanceByType(IEnumerable<IWorkerPool> types);
}

internal sealed class EvenWorkerPoolBalancer : IWorkerPoolBalancer
{
    private readonly int _maxConnections;

    public EvenWorkerPoolBalancer(int maxConnections)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConnections);
        _maxConnections = maxConnections;
    }

    public Dictionary<IWorkerPool, int> CalculateBalanceByType(IEnumerable<IWorkerPool> types)
    {
        var workerPools = types.ToArray();
        var result = new Dictionary<IWorkerPool, int>();
        var count = workerPools.Length;
        var baseShare = _maxConnections / count;
        var remainder = _maxConnections % count;

        for (var i = 0; i < count; i++)
        {
            var workerPool = workerPools[i];
            var allocation = baseShare + (i < remainder ? 1 : 0);
            result[workerPool] = allocation;
        }

        return result;
    }
}

internal sealed class ConstantWorkerPoolBalancer(int constant) : IWorkerPoolBalancer
{
    public Dictionary<IWorkerPool, int> CalculateBalanceByType(IEnumerable<IWorkerPool> types) =>
        types.ToDictionary(x => x, _ => constant);
}
