using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql;

internal static class MigrationSqlLoader
{
    private const string BaseNamespace = "Digdir.Domain.Dialogporten.Infrastructure.Persistence.Sql";

    /// <summary>
        /// Loads the contents of multiple embedded SQL resources identified by the given relative paths.
        /// </summary>
        /// <param name="relativePaths">Relative paths to embedded SQL resources; path separators may be slashes or backslashes. Multiple paths are allowed.</param>
        /// <returns>An enumerable of resource contents corresponding to each provided relative path, in the same order as the input.</returns>
        internal static IEnumerable<string> LoadAll(params string[] relativePaths) =>
        relativePaths.Select(Load);

    /// <summary>
    /// Loads an embedded SQL resource identified by a relative path and returns its contents.
    /// </summary>
    /// <param name="relativePath">Relative path to the embedded SQL resource; forward or backward slashes will be normalized to resource name segments.</param>
    /// <returns>The full text content of the embedded SQL resource.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="relativePath"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the embedded resource for the constructed resource name cannot be found.</exception>
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

    /// <summary>
        /// Convert a relative file path into a resource name segment by replacing path separators with dots.
        /// </summary>
        /// <param name="relativePath">A file path segment using '/' or '\' as separators.</param>
        /// <returns>The input string with all '/' and '\' characters replaced by '.'.</returns>
        private static string Normalize(string relativePath) =>
        relativePath.Replace('\\', '.').Replace('/', '.');
}