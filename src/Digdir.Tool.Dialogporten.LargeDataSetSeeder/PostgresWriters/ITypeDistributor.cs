namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.PostgresWriters;

internal interface ITypeDistributor
{
    Dictionary<Type, int> GetDistribution(int maxConnections, IEnumerable<Type> types);
}

internal sealed class EvenTypeDistributor : ITypeDistributor
{
    public static EvenTypeDistributor Instance { get; } = new EvenTypeDistributor();
    private EvenTypeDistributor() { }
    public Dictionary<Type, int> GetDistribution(int maxConnections, IEnumerable<Type> types)
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
}

internal sealed class ConstantTypeDistributor(int constant) : ITypeDistributor
{
    public Dictionary<Type, int> GetDistribution(int maxConnections, IEnumerable<Type> types)
    {
        var typeList = types.ToArray();
        var totalRequired = typeList.Length * constant;
        return totalRequired > maxConnections
            ? throw new ArgumentException("Not enough connections to allocate the constant per type.")
            : typeList.ToDictionary(x => x, _ => constant);
    }
}
