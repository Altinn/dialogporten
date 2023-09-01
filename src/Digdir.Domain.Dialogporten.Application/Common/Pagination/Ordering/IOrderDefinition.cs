﻿using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Digdir.Domain.Dialogporten.Application.Common.Pagination.Ordering;

public interface IOrderDefinition<TTarget>
{
    public static abstract void Configure(IOrderBuilder<TTarget> options);
}
public interface IOrderBuilder<TTarget>
{
    IOrderDefaultBuilder<TTarget> AddId(Expression<Func<TTarget, object?>> expression);
}

public interface IOrderDefaultBuilder<TTarget>
{
    IOrderOptionsBuilder<TTarget> AddDefault(string key, Expression<Func<TTarget, object?>> expression);
}


public interface IOrderOptionsBuilder<TTarget>
{
    IOrderOptionsBuilder<TTarget> AddOption(string key, Expression<Func<TTarget, object?>> expression);
}

internal class OrderOptionsBuilder<TTarget> : IOrderBuilder<TTarget>, IOrderDefaultBuilder<TTarget>, IOrderOptionsBuilder<TTarget>
{
    private readonly Dictionary<string, OrderSelector<TTarget>> _optionByKey = new(StringComparer.InvariantCultureIgnoreCase);
    private string? _defaultKey;

    public IOrderDefaultBuilder<TTarget> AddId(Expression<Func<TTarget, object?>> expression)
    {
        _defaultKey = null;
        _optionByKey.Clear();
        _optionByKey[PaginationConstants.OrderIdKey] = new(expression);
        return this;
    }

    public IOrderOptionsBuilder<TTarget> AddDefault([NotNull] string key, Expression<Func<TTarget, object?>> expression)
    {
        _defaultKey = key;
        _optionByKey[_defaultKey] = new(expression);
        return this;
    }

    public IOrderOptionsBuilder<TTarget> AddOption([NotNull] string key, Expression<Func<TTarget, object?>> expression)
    {
        _optionByKey[key] = new(expression);
        return this;
    }

    internal OrderOptionsBuilder<TTarget> Configure<TOrderDefinition>()
        where TOrderDefinition : IOrderDefinition<TTarget>
    {
        TOrderDefinition.Configure(this);
        return this;
    }

    internal OrderOptions<TTarget> Build()
    {
        if (_defaultKey is null)
        {
            throw new InvalidOperationException("No default value is specified.");
        }

        return new OrderOptions<TTarget>(_defaultKey, _optionByKey);
    }
}

public class OrderOptions<TTarget>
{
    private readonly string _defaultKey;
    private readonly Dictionary<string, OrderSelector<TTarget>> _optionByKey;

    public OrderOptions(string defaultKey, Dictionary<string, OrderSelector<TTarget>> optionByKey)
    {
        _defaultKey = defaultKey;
        _optionByKey = optionByKey;
    }

    internal bool TryParse(string value, [NotNullWhen(true)] out Order<TTarget>? result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        result = value.Split(PaginationConstants.OrderDelimiter, StringSplitOptions.TrimEntries) switch
        {
            // eks: createdAt
            [var key]
                when _optionByKey.TryGetValue(key, out var expression)
                => new(key, expression),

            // eks: createdAt_desc
            [var key, var direction]
                when _optionByKey.TryGetValue(key, out var expression)
                && Enum.TryParse<OrderDirection>(direction, ignoreCase: true, out var dirEnum)
                => new(key, expression, dirEnum),

            //// eks: createdAt_desc_continuationToken
            //[var key, var direction, var continuationToken]
            //    when _optionByKey.TryGetValue(key, out var expression)
            //    && Enum.TryParse<OrderDirection>(direction, out var dirEnum)
            //    && TryParseExtensions.TryParse(expression.CleanedBody.Type, continuationToken, out var ct)
            //    => Create(key, expression, dirEnum, ct),

            _ => null
        };

        return result is not null;
    }

    internal Order<TTarget> GetId() => new(PaginationConstants.OrderIdKey, _optionByKey[PaginationConstants.OrderIdKey]);
    internal Order<TTarget> GetDefault() => new(_defaultKey, _optionByKey[_defaultKey]);
    internal bool TryGetOption(string? key, [NotNullWhen(true)] out OrderSelector<TTarget>? option)
    {
        option = default;
        return key is not null && _optionByKey.TryGetValue(key, out option);
    }
}

public record OrderSelector<TTarget>(Expression<Func<TTarget, object?>> Expression, Lazy<Func<TTarget, object?>> Compiled, Expression Body)
{
    public OrderSelector(Expression<Func<TTarget, object?>> expression) : this(expression, new(expression.Compile), RemoveConvertWrapper(expression.Body)) { }

    private static Expression RemoveConvertWrapper(Expression body) =>
        body.NodeType == ExpressionType.Convert && body is UnaryExpression unaryExpression
            ? unaryExpression.Operand
            : body;
}

public class Order<TTarget>
{
    private readonly OrderSelector<TTarget> _expressionCache;

    public string Key { get; }
    public OrderDirection Direction { get; }
    public Expression<Func<TTarget, object?>> SelectorExpression => _expressionCache.Expression;
    public Expression SelectorBody => _expressionCache.Body;
    public Func<TTarget, object?> CompiledSelector => _expressionCache.Compiled.Value;

    public Order(string key, OrderSelector<TTarget> expressionCache, OrderDirection direction = PaginationConstants.DefaultOrderDirection)
    {
        _expressionCache = expressionCache;
        Key = key;
        Direction = direction;
    }
}
