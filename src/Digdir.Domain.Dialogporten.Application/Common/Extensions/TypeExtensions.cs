namespace Digdir.Domain.Dialogporten.Application.Common.Extensions;

public static class TypeExtensions
{
    public static bool IsNullableType(this Type type)
        => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
}
