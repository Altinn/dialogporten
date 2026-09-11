using System.Collections;
using System.Reflection;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.Mapping;

/// <summary>
/// Deterministic, reflection-based object graph filler used to build fully populated source
/// objects for the mapping snapshot tests.
///
/// Every leaf value is derived from a monotonically increasing counter, so:
/// - output is stable across runs (given a fixed build), making it safe to snapshot, and
/// - each populated property gets a distinct, traceable value, so accidental field
///   cross-wiring in a mapper shows up as a changed value in the snapshot.
///
/// A single <see cref="ObjectFiller"/> instance shares one counter; use the same instance when
/// filling multiple objects that should have distinct values (e.g. items in a list).
/// </summary>
internal sealed class ObjectFiller
{
    private const int CollectionSize = 2;
    private const int MaxDepth = 15;
    private const int MaxTypeOccurrencesOnPath = 2;

    private int _counter;

    /// <summary>Fills a single instance of <typeparamref name="T"/> using a fresh counter.</summary>
    public static T Fill<T>() => new ObjectFiller().Create<T>();

    /// <summary>Fills an instance of <typeparamref name="T"/> using this instance's shared counter.</summary>
    public T Create<T>() => (T)CreateValue(typeof(T), new Dictionary<Type, int>(), 0)!;

    private object? CreateValue(Type type, Dictionary<Type, int> path, int depth, string? memberName = null)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return CreateString(memberName);
        if (underlying == typeof(bool)) return Next() % 2 == 1;
        if (underlying == typeof(Guid)) return new Guid(Next(), 0, 0, new byte[8]);
        if (underlying == typeof(DateTimeOffset)) return BaseDate.AddSeconds(Next());
        if (underlying == typeof(DateTime)) return BaseDate.UtcDateTime.AddSeconds(Next());
        if (underlying == typeof(DateOnly)) return DateOnly.FromDateTime(BaseDate.UtcDateTime).AddDays(Next());
        if (underlying == typeof(TimeSpan)) return TimeSpan.FromSeconds(Next());
        if (underlying == typeof(Uri)) return new Uri($"https://example.com/{Next()}");
        if (underlying == typeof(char)) return (char)('A' + (Next() % 26));
        if (underlying.IsEnum)
        {
            var values = Enum.GetValues(underlying);
            return values.GetValue(Next() % values.Length);
        }

        if (IsNumeric(underlying))
        {
            return Convert.ChangeType(Next(), underlying, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (TryGetElementType(underlying, out var elementType))
        {
            return CreateCollection(elementType!, path, depth);
        }

        return CreateComplex(underlying, path, depth);
    }

    private object? CreateComplex(Type type, Dictionary<Type, int> path, int depth)
    {
        if (depth >= MaxDepth || Occurrences(path, type) >= MaxTypeOccurrencesOnPath)
        {
            return null;
        }

        var instance = Activator.CreateInstance(type, nonPublic: true)
            ?? throw new InvalidOperationException($"Could not instantiate {type.FullName}");

        path[type] = Occurrences(path, type) + 1;
        try
        {
            foreach (var property in GetWritableProperties(type))
            {
                var value = CreateValue(property.PropertyType, path, depth + 1, property.Name);
                property.SetValue(instance, value);
            }
        }
        finally
        {
            path[type] = Occurrences(path, type) - 1;
        }

        return instance;
    }

    private object CreateCollection(Type elementType, Dictionary<Type, int> path, int depth)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        var underlyingElement = Nullable.GetUnderlyingType(elementType) ?? elementType;

        // Stop generating elements when the element type is already recursing (e.g. self-referential
        // sub-parties) or when we've hit the depth cap; leaving an empty (non-null) list keeps output
        // deterministic while still exercising the collection mapping.
        if (depth < MaxDepth && Occurrences(path, underlyingElement) < MaxTypeOccurrencesOnPath)
        {
            for (var i = 0; i < CollectionSize; i++)
            {
                list.Add(CreateValue(elementType, path, depth + 1));
            }
        }

        return list;
    }

    private static IEnumerable<PropertyInfo> GetWritableProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is { CanWrite: true } && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.MetadataToken);

    private static bool TryGetElementType(Type type, out Type? elementType)
    {
        elementType = null;
        if (type == typeof(string))
        {
            return false;
        }

        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return elementType is not null;
        }

        var enumerable = new[] { type }
            .Concat(type.GetInterfaces())
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerable is null)
        {
            return false;
        }

        elementType = enumerable.GetGenericArguments()[0];
        return true;
    }

    private static bool IsNumeric(Type type) =>
        Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16
            or TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64
            or TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    private static int Occurrences(Dictionary<Type, int> path, Type type) =>
        path.GetValueOrDefault(type);

    private string CreateString(string? memberName)
    {
        // Language/culture-code members are normalized on set (Localization.NormalizeCultureCode
        // lower-cases and keeps only the segment before the first '-'), which would collapse the
        // default "str-{N}" values to a single "str" and hide field cross-wiring. Emit hyphen-free
        // values that survive normalization while staying distinct.
        if (memberName is not null &&
            (memberName.Contains("LanguageCode", StringComparison.OrdinalIgnoreCase) ||
             memberName.Contains("CultureCode", StringComparison.OrdinalIgnoreCase)))
        {
            return $"lang{Next()}";
        }

        return $"str-{Next()}";
    }

    private int Next() => ++_counter;

    private static readonly DateTimeOffset BaseDate = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
}
