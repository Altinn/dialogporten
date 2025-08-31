using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal interface IEntityGenerator<out T> where T : class
{
    static abstract IEnumerable<T> GenerateEntities(IEnumerable<DialogTimestamp> timestamps);
}

[SuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
[SuppressMessage("Trimming", "IL2070:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
internal static class EntityGeneratorExtensions
{
    public static readonly FrozenDictionary<Type, Func<IEnumerable<DialogTimestamp>, IEnumerable<object>>> Generators
        = BuildGeneratorDelegates(Assembly.GetEntryAssembly()!);

    private static FrozenDictionary<Type, Func<IEnumerable<DialogTimestamp>, IEnumerable<object>>> BuildGeneratorDelegates(
        params IEnumerable<Assembly> assemblies)
    {
        var dict = new Dictionary<Type, Func<IEnumerable<DialogTimestamp>, IEnumerable<object>>>();

        var generators = assemblies
            .DefaultIfEmpty(Assembly.GetCallingAssembly())
            .SelectMany(x => x.DefinedTypes)
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(IsEntityGeneratorInterface)
                .Select(iface => (Type: t, Interface: iface)));

        foreach (var (type, iface) in generators)
        {
            var returnType = iface.GetGenericArguments()[0]; // T in IEntityGenerator<T>
            var generator = BuildDelegate(type, iface);
            dict[returnType] = generator; // ⚠ overwrites if multiple generators for same return type
        }

        return dict.ToFrozenDictionary();
    }

    private static bool IsEntityGeneratorInterface(Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEntityGenerator<>);

    private static Func<IEnumerable<DialogTimestamp>, IEnumerable<object>> BuildDelegate(Type implType, Type ifaceType)
    {
        const string methodName = nameof(IEntityGenerator<object>.GenerateEntities);

        // Get static explicit interface method implementation
        var method = implType.GetInterfaceMap(ifaceType).TargetMethods
            .FirstOrDefault(m => m.Name.Contains(methodName))
                     ?? throw new InvalidOperationException(
                         $"Type {implType.FullName} does not implement {ifaceType.FullName}.{methodName}");

        // Parameter: IEnumerable<DialogTimestamp>
        var param = Expression.Parameter(typeof(IEnumerable<DialogTimestamp>), "timestamps");

        // Call method (static, so no instance)
        var call = Expression.Call(null, method, param);

        // Wrap return IEnumerable<T> -> IEnumerable<object>
        // var castCall = Expression.Call(typeof(Enumerable), nameof(Enumerable.Cast), [typeof(object)], call);

        return Expression.Lambda<Func<IEnumerable<DialogTimestamp>, IEnumerable<object>>>(call, param).Compile();
    }
}

