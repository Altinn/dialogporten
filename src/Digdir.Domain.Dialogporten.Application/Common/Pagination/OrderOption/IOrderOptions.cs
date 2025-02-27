using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;

namespace Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;

public interface IOrderOptions<TTarget>
{
    Order<TTarget> DefaultOrder { get; }
    Order<TTarget> IdOrder { get; }
    bool TryGetSelector(string? key, [NotNullWhen(true)] out OrderSelector<TTarget>? option);
    bool TryParseOrder(string? value, [NotNullWhen(true)] out Order<TTarget>? result);
}

internal static class OrderOptions<TOrderDefinition, TTarget>
    where TOrderDefinition : IOrderDefinition<TTarget>
{
    public static readonly IOrderOptions<TTarget> Value = TOrderDefinition.Configure(new OrderOptionsBuilder<TTarget>());
}

internal sealed class OrderOptions<TTarget> : IOrderOptions<TTarget>
{
    private readonly Dictionary<string, OrderSelector<TTarget>> _optionByKey;

    public Order<TTarget> DefaultOrder { get; }
    public Order<TTarget> IdOrder { get; }

    public OrderOptions(string defaultKey, Dictionary<string, OrderSelector<TTarget>> optionByKey)
    {
        _optionByKey = optionByKey;
        DefaultOrder = new(defaultKey, _optionByKey[defaultKey]);
        IdOrder = new(PaginationConstants.OrderIdKey, _optionByKey[PaginationConstants.OrderIdKey]);
    }

    public bool TryParseOrder(string? value, [NotNullWhen(true)] out Order<TTarget>? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        result = value.Split(PaginationConstants.OrderDelimiter, StringSplitOptions.TrimEntries) switch
        {
            // eks: createdAt
            [var key] when _optionByKey.TryGetValue(key, out var expression) => new(key, expression),
            // eks: createdAt_desc
            [var key, var direction]
            when _optionByKey.TryGetValue(key, out var expression)
            && Enum.TryParse<OrderDirection>(direction, ignoreCase: true, out var dirEnum) => new(key, expression, dirEnum),
            _ => null
        };

        return result is not null;
    }

    public bool TryGetSelector(string? key, [NotNullWhen(true)] out OrderSelector<TTarget>? option)
    {
        option = default;
        return key is not null && _optionByKey.TryGetValue(key, out option);
    }
}
