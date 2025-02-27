using System.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

internal static class TypeNameConverter
{
    private const string Of = "Of";
    private const string And = "And";
    private const string NamespaceClassSeparator = "_";

    private static readonly string[] ExcludedClassPostfixes = ["Endpoint", "+Values", "Dto"];
    private static readonly string[] ExcludedClassPrefixes = ["Create", "Update", "Delete", "Patch"];
    private static readonly string[] ExcludedNamespacePostfixes = [];
    private static readonly string[] ExcludedNamespacePrefixes =
    [
        "FastEndpoints",
        "Digdir.Domain.Dialogporten.Domain.",
        "Digdir.Domain.Dialogporten.WebApi.Endpoints.",
        "Digdir.Domain.Dialogporten.Application.Features.",
        "Microsoft.AspNetCore."
    ];

    internal static string ToShortName(Type type)
    {
        var index = 0;
        Span<char> finalName = stackalloc char[type.FullName!.Length];
        ToShortName(type, finalName, ref index);
        return finalName[..index].ToString();
    }

    private static void ToShortName(Type type, Span<char> finalName, ref int index)
    {
        if (type.IsGenericType)
        {
            WriteGenericType(type, finalName, ref index);
            return;
        }

        WriteNonGenericType(type, finalName, ref index);
    }

    private static void WriteGenericType(Type type, Span<char> finalName, ref int index)
    {
        var genericName = type.Name.AsSpan();
        genericName[..genericName.IndexOf('`')].CopyTo(finalName, ref index);
        Of.AsSpan().CopyTo(finalName, ref index);

        using var typeArguments = type
            .GetGenericArguments()
            .AsEnumerable()
            .GetEnumerator();

        if (!typeArguments.MoveNext())
        {
            throw new UnreachableException("Assumption: Generic type has at least one generic argument.");
        }

        ToShortName(typeArguments.Current, finalName, ref index);
        while (typeArguments.MoveNext())
        {
            And.AsSpan().CopyTo(finalName, ref index);
            ToShortName(typeArguments.Current, finalName, ref index);
        }
    }

    private static void WriteNonGenericType(Type type, Span<char> finalName, ref int index)
    {
        // If the input type is a generic parameter, use its name as is. For example, in the
        // generic type List<T>, T is a generic parameter. Concrete types are not generic 
        // parameters and will be processed as usual. For example in List<int>, int is a
        // concrete type, and not a generic parameter.
        if (type.IsGenericTypeParameter)
        {
            type.Name.AsSpan().CopyTo(finalName, ref index);
            return;
        }

        type.FullName
            .AsSpan()
            .SplitNamespaceAndClassName(out var namespaceName, out var className);
        className = className
            .ExcludePrefix(ExcludedClassPrefixes)
            .ExcludePostfix(ExcludedClassPostfixes);
        namespaceName = namespaceName
            .ExcludePrefix(ExcludedNamespacePrefixes)
            .ExcludePostfix(ExcludedNamespacePostfixes);
        var namespaceWritten = false;

        // Copy namespace without '.'
        foreach (var @char in namespaceName)
        {
            if (@char != '.')
            {
                finalName[index++] = @char;
                namespaceWritten = true;
            }
        }

        // Add separator between namespace and class name
        if (namespaceWritten)
        {
            NamespaceClassSeparator.AsSpan().CopyTo(finalName, ref index);
        }

        // Copy class name
        className.CopyTo(finalName, ref index);
    }

    private static ReadOnlySpan<char> ExcludePrefix(this ReadOnlySpan<char> name, IEnumerable<string> prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (name.StartsWith(prefix) &&
                (name.Length == prefix.Length ||
                 (name.Length > prefix.Length && (name[prefix.Length] < 'a' || name[prefix.Length] > 'z'))))
            {
                name = name[prefix.Length..];
                break;
            }
        }

        return name;
    }

    private static ReadOnlySpan<char> ExcludePostfix(this ReadOnlySpan<char> name, IEnumerable<string> postfixes)
    {
        foreach (var postfix in postfixes)
        {
            if (name.EndsWith(postfix))
            {
                name = name[..^postfix.Length];
                break;
            }
        }

        return name;
    }

    private static void SplitNamespaceAndClassName(this ReadOnlySpan<char> fullName, out ReadOnlySpan<char> namespaceName, out ReadOnlySpan<char> className)
    {
        const int notFound = -1;
        var classNamespaceSeparatorIndex = fullName.LastIndexOf('.');
        className = classNamespaceSeparatorIndex == notFound
            ? fullName
            : fullName[(classNamespaceSeparatorIndex + 1)..];
        namespaceName = classNamespaceSeparatorIndex == notFound
            ? ReadOnlySpan<char>.Empty
            : fullName[..classNamespaceSeparatorIndex];
    }

    private static void CopyTo(this ReadOnlySpan<char> source, Span<char> destination, ref int index)
    {
        source.CopyTo(destination[index..]);
        index += source.Length;
    }
}
