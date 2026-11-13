namespace Digdir.Domain.Dialogporten.Application.Common;

/// <summary>
/// Abstraction around the time that is to be used for all time
/// related DB and event fields within the same transaction.
/// </summary>
public interface ITransactionTime
{
    DateTimeOffset Value { get; }
}

/// <summary>
/// This will wait to the last possible moment to get the
/// current time and use that as the transaction time.
/// </summary>
internal sealed class TransactionTime : ITransactionTime
{
    private readonly Lazy<DateTimeOffset> _lazyValue;
    private DateTimeOffset? _inheritedValue;
    public DateTimeOffset Value => _lazyValue.Value;
    public TransactionTime(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        _lazyValue = new(() => _inheritedValue ?? clock.UtcNow);
    }

    /// <summary>
    /// Makes this transaction time use the value of another transaction time, typically
    /// one from a parent scope. Must be called before <see cref="Value"/> is read.
    /// </summary>
    internal void Inherit(DateTimeOffset value)
    {
        if (_lazyValue.IsValueCreated)
        {
            throw new InvalidOperationException("Cannot inherit a transaction time that has already been used.");
        }

        _inheritedValue = value;
    }
}
