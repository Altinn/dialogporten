using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;

internal static class FluentValidationDateTimeOffsetExtensions
{
    public const string InPastMessage = "'{PropertyName}' must be in the past.";
    public const string InFutureMessage = "'{PropertyName}' must be in the future.";

    public const string InPastOfMessage = "'{PropertyName}' must be before '{ComparisonProperty}'.";
    public const string InFutureOfMessage = "'{PropertyName}' must be after '{ComparisonProperty}'.";

    public const string NotMaxValueMessage = "'{PropertyName}' must be a valid date and cannot be the maximum date value.";

    private static readonly TimeSpan FutureTolerance = TimeSpan.FromSeconds(15);

    // Npgsql maps DateTimeOffset.MaxValue to the PostgreSQL special timestamp value
    // 'infinity', which breaks downstream date arithmetic (e.g. the search index sort key
    // casts EXTRACT(EPOCH ...) to bigint and fails on infinity). Reject it at the input
    // boundary. DateTimeOffset.MinValue/default is intentionally allowed: it is the
    // "not supplied, use current time" sentinel handled by the ICreatableEntity hook.
    public static IRuleBuilderOptions<T, DateTimeOffset?> IsNotMaxValue<T>(this IRuleBuilder<T, DateTimeOffset?> ruleBuilder)
    {
        return ruleBuilder
            .NotEqual(DateTimeOffset.MaxValue)
            .WithMessage(NotMaxValueMessage);
    }

    public static IRuleBuilderOptions<T, DateTimeOffset> IsNotMaxValue<T>(this IRuleBuilder<T, DateTimeOffset> ruleBuilder)
    {
        return ruleBuilder
            .NotEqual(DateTimeOffset.MaxValue)
            .WithMessage(NotMaxValueMessage);
    }

    public static IRuleBuilderOptions<T, DateTimeOffset?> IsInFuture<T>(this IRuleBuilder<T, DateTimeOffset?> ruleBuilder, IClock clock)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(clock.UtcNowOffset)
            .WithMessage(InFutureMessage);
    }

    public static IRuleBuilderOptions<T, DateTimeOffset> IsInFuture<T>(this IRuleBuilder<T, DateTimeOffset> ruleBuilder, IClock clock)
    {
        return ruleBuilder
            .GreaterThanOrEqualTo(clock.UtcNowOffset)
            .WithMessage(InFutureMessage);
    }

    public static IRuleBuilderOptions<T, DateTimeOffset?> IsInPast<T>(this IRuleBuilder<T, DateTimeOffset?> ruleBuilder, IClock clock)
    {
        return ruleBuilder
            .LessThanOrEqualTo(clock.UtcNowOffset.Add(FutureTolerance))
            .WithMessage(InPastMessage);
    }

    public static IRuleBuilderOptions<T, DateTimeOffset> IsInPast<T>(this IRuleBuilder<T, DateTimeOffset> ruleBuilder, IClock clock)
    {
        return ruleBuilder
            .LessThanOrEqualTo(clock.UtcNowOffset.Add(FutureTolerance))
            .WithMessage(InPastMessage);
    }
}
