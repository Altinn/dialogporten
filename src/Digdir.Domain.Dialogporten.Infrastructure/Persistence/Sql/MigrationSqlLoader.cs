using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;

internal static class MigrationSqlLoader
{
    private const string BaseNamespace = "Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql";

    internal static IEnumerable<string> LoadAll(params string[] relativePaths) =>
        relativePaths.Select(Load);

    internal static string Load(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path must be provided.", nameof(relativePath));
        }

        var resourceName = $"{BaseNamespace}.{Normalize(relativePath)}";
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Embedded SQL resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string Normalize(string relativePath) =>
        relativePath.Replace('\\', '.').Replace('/', '.');
}
