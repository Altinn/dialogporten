using System.Security.Cryptography;
using System.Text;

namespace Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;

internal static class CacheKeyHash
{
    /// <summary>
    /// Builds a cache key of the form <c>{prefix}{sha256-lowercase-hex(rawKey)}</c>. Shared so all per-caller
    /// cache keys in this namespace use one hashing + hex-casing convention.
    /// </summary>
    public static string Build(string prefix, string rawKey) =>
        prefix + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
}
