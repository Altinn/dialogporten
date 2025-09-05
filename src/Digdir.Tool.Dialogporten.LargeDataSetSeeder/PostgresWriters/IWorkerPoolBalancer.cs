using System.Collections.ObjectModel;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal interface IWorkerPool
{
    Type Type { get; }
    int WorkerCount { get; }
    int WorkerLoad { get; }
}

internal interface IWorkerPoolBalancer
{
    ReadOnlyDictionary<IWorkerPool, int> CalculateBalanceByType(IEnumerable<IWorkerPool> workerPools);
}

internal sealed class EvenWorkerPoolBalancer : IWorkerPoolBalancer
{
    private readonly int _maxConnections;

    public EvenWorkerPoolBalancer(int maxConnections)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConnections);
        _maxConnections = maxConnections;
    }

    public ReadOnlyDictionary<IWorkerPool, int> CalculateBalanceByType(IEnumerable<IWorkerPool> workerPools)
    {
        var wp = workerPools.ToArray();
        var result = new Dictionary<IWorkerPool, int>();
        var count = wp.Length;
        var baseShare = _maxConnections / count;
        var remainder = _maxConnections % count;

        for (var i = 0; i < count; i++)
        {
            var workerPool = wp[i];
            var allocation = baseShare + (i < remainder ? 1 : 0);
            result[workerPool] = allocation;
        }

        return result.AsReadOnly();
    }
}

internal sealed class ConstantWorkerPoolBalancer(int constant) : IWorkerPoolBalancer
{
    public ReadOnlyDictionary<IWorkerPool, int> CalculateBalanceByType(IEnumerable<IWorkerPool> workerPools) =>
        workerPools.ToDictionary(x => x, _ => constant).AsReadOnly();
}
