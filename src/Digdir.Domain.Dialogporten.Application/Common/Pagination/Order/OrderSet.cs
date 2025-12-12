using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using System.Diagnostics.CodeAnalysis;

namespace Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;

public interface IOrderSet<TTarget>
{
    IReadOnlyCollection<Order<TTarget>> Orders { get; }
    string? GetContinuationTokenFrom(TTarget? t);
    string GetOrderString();
}

public sealed class OrderSet<TOrderDefinition, TTarget> : IOrderSet<TTarget>
    where TOrderDefinition : IOrderDefinition<TTarget>
{
    private static readonly OrderComparer _orderComparer = new();

    public static readonly OrderSet<TOrderDefinition, TTarget> Default = new(
    [
        OrderOptions<TOrderDefinition, TTarget>.Value.DefaultOrder,
        OrderOptions<TOrderDefinition, TTarget>.Value.IdOrder
    ]);

    public IReadOnlyCollection<Order<TTarget>> Orders { get; }

    public OrderSet(IReadOnlyCollection<Order<TTarget>> orders)
    {
        Orders = orders;
    }

    public static bool TryParse(string? value, [NotNullWhen(true)] out OrderSet<TOrderDefinition, TTarget>? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // eks: orderBy=createdAt_asc,updatedAt_desc
        var orders = new HashSet<Order<TTarget>>(_orderComparer);
        foreach (var orderAsString in value.Split(PaginationConstants.OrderSetDelimiter, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!OrderOptions<TOrderDefinition, TTarget>.Value.TryParseOrder(orderAsString, out var order) ||
                !orders.Add(order))
            {
                return false;
            }
        }

        var orderList = orders.ToList();

        // Ensure ID order is last
        var idOrder = orderList
            .FirstOrDefault(x => x.Key == PaginationConstants.OrderIdKey)
            ?? OrderOptions<TOrderDefinition, TTarget>.Value.IdOrder;

        orderList.Remove(idOrder);

        orderList.Add(idOrder);

        // Ensure at least two sorting parameters
        if (orderList.Count == 1)
        {
            orderList.Insert(0, OrderOptions<TOrderDefinition, TTarget>.Value.DefaultOrder);
        }

        result = new OrderSet<TOrderDefinition, TTarget>(orderList);
        return true;
    }

    public string? GetContinuationTokenFrom(TTarget? t)
    {
        if (t is null)
        {
            return null;
        }

        var continuationTokenParts = Orders
            .Select(x =>
            {
                var value = x.GetSelector().Compiled.Value.Invoke(t) switch
                {
                    null => string.Empty,
                    string s => s,
                    DateTimeOffset d => d.UtcDateTime.ToString("o"),
                    DateTime d => d.ToString("o"),
                    var o => o.ToString()
                };
                return $"{x.Key.ToLowerInvariant()}{PaginationConstants.ContinuationTokenDelimiter}{value}";
            });
        return string.Join(PaginationConstants.ContinuationTokenSetDelimiter, continuationTokenParts);
    }

    public string GetOrderString()
    {
        var orderParts = Orders.Select(x => $"{x.Key.ToLowerInvariant()}{PaginationConstants.OrderDelimiter}{x.Direction.ToString().ToLowerInvariant()}");
        return string.Join(PaginationConstants.OrderSetDelimiter, orderParts);
    }

    private sealed class OrderComparer : IEqualityComparer<Order<TTarget>>
    {
        public bool Equals(Order<TTarget>? x, Order<TTarget>? y)
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

        public int GetHashCode(Order<TTarget> obj) => obj.Key.GetHashCode();
    }
}
