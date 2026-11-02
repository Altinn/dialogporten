using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;

internal static class FluentValidationDateTimeOffsetExtensions
{
    public const string InPastMessage = "'{PropertyName}' must be in the past.";
    public const string InFutureMessage = "'{PropertyName}' must be in the future.";

    public const string InPastOfMessage = "'{PropertyName}' must be before '{ComparisonProperty}'.";
    public const string InFutureOfMessage = "'{PropertyName}' must be after '{ComparisonProperty}'.";

    public const string FiniteMessage = "'{PropertyName}' must be a valid, finite date.";

    private static readonly TimeSpan FutureTolerance = TimeSpan.FromSeconds(15);

    // DateTimeOffset.MinValue/MaxValue are mapped by Npgsql to the PostgreSQL special
    // timestamp values '-infinity'/'infinity', which cannot be used in date arithmetic
    // downstream (e.g. the search index sort key casts EXTRACT(EPOCH ...) to bigint and
    // fails on infinity). Reject them at the input boundary.
    public static IRuleBuilderOptions<T, DateTimeOffset?> IsFinite<T>(this IRuleBuilder<T, DateTimeOffset?> ruleBuilder)
    {
        return ruleBuilder
            .Must(x => x is null || (x.Value != DateTimeOffset.MinValue && x.Value != DateTimeOffset.MaxValue))
            .WithMessage(FiniteMessage);
    }

    public static IRuleBuilderOptions<T, DateTimeOffset> IsFinite<T>(this IRuleBuilder<T, DateTimeOffset> ruleBuilder)
    {
        return ruleBuilder
            .Must(x => x != DateTimeOffset.MinValue && x != DateTimeOffset.MaxValue)
            .WithMessage(FiniteMessage);
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
