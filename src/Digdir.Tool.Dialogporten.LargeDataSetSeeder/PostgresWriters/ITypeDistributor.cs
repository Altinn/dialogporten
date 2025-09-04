namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal interface ITypeDistributor
{
    Dictionary<Type, int> GetDistribution(IEnumerable<Type> types);
}

internal sealed class EvenTypeDistributor : ITypeDistributor
{
    private readonly int _maxConnections;

    public EvenTypeDistributor(int maxConnections)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConnections);
        _maxConnections = maxConnections;
    }

    public Dictionary<Type, int> GetDistribution(IEnumerable<Type> types)
    {
        var typeList = types.ToArray();
        var result = new Dictionary<Type, int>();
        var count = typeList.Length;
        var baseShare = _maxConnections / count;
        var remainder = _maxConnections % count;

        for (var i = 0; i < count; i++)
        {
            var allocation = baseShare + (i < remainder ? 1 : 0);
            result[typeList[i]] = allocation;
        }

        return result;
    }
}

internal sealed class ConstantTypeDistributor(int constant) : ITypeDistributor
{
    public Dictionary<Type, int> GetDistribution(IEnumerable<Type> types) =>
        types.ToDictionary(x => x, _ => constant);
}
