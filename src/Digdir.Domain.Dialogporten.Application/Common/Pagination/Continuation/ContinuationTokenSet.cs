using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;

namespace Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;

public interface IContinuationTokenSet
{
    IReadOnlyCollection<ContinuationToken> Tokens { get; }
    string Raw { get; }
}

public sealed class ContinuationTokenSet<TOrderDefinition, TTarget> : IContinuationTokenSet
    where TOrderDefinition : IOrderDefinition<TTarget>
{
    private static readonly ContinuationTokenComparer _tokenComparer = new();

    public IReadOnlyCollection<ContinuationToken> Tokens { get; }
    public string Raw { get; }

    public ContinuationTokenSet(IReadOnlyCollection<ContinuationToken> tokens, string raw)
    {
        Tokens = tokens;
        Raw = raw;
    }

    public static bool TryParse(string? value, out ContinuationTokenSet<TOrderDefinition, TTarget>? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var tokens = new HashSet<ContinuationToken>(_tokenComparer);
        var continuationTokens = value
            .Split(PaginationConstants.ContinuationTokenSetDelimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split(PaginationConstants.ContinuationTokenDelimiter, StringSplitOptions.TrimEntries) switch
                {
                    [var key, var ctAsString]
                    when OrderOptions<TOrderDefinition, TTarget>.Value.TryGetSelector(key, out var option)
                    && TryParseExtensions.TryParse(option.Body.Type, ctAsString, out var ct) => new ContinuationToken(key, ct, option.Body.Type),
                    _ => null
                });

        foreach (var continuationToken in continuationTokens)
        {
            if (continuationToken is null || !tokens.Add(continuationToken))
            {
                return false;
            }
        }

        result = new(tokens, value);
        return true;
    }

    private sealed class ContinuationTokenComparer : IEqualityComparer<ContinuationToken>
    {
        public bool Equals(ContinuationToken? x, ContinuationToken? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.Key == y.Key;
        }

        public int GetHashCode([DisallowNull] ContinuationToken obj) => obj.Key.GetHashCode();
    }
}
