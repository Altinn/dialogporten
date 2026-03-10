using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn;

internal static class LocalizationExtensions
{
    public static IReadOnlyList<ResourceLocalization> ToLocalizations(this IDictionary<string, string>? values)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<ResourceLocalization>();
        }

        return values
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => new ResourceLocalization(x.Key.Trim().ToLowerInvariant(), x.Value))
            .ToArray();
    }
}
