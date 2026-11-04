using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;

internal static class FluentValidationDateTimeOffsetExtensions
{
    public const string InPastMessage = "'{PropertyName}' must be in the past.";
    public const string InFutureMessage = "'{PropertyName}' must be in the future.";

    public const string InPastOfMessage = "'{PropertyName}' must be before '{ComparisonProperty}'.";
    public const string InFutureOfMessage = "'{PropertyName}' must be after '{ComparisonProperty}'.";

    public const string FiniteMessage = "'{PropertyName}' must be a valid date within the supported range.";

    private static readonly TimeSpan FutureTolerance = TimeSpan.FromSeconds(15);

    // Npgsql maps DateTimeOffset.MaxValue/MinValue to the PostgreSQL special timestamp values
    // 'infinity'/'-infinity', which break downstream date arithmetic (e.g. the search index sort
    // key casts EXTRACT(EPOCH ...) to bigint and fails on infinity). Reject both at the input
    // boundary.
    //
    // Only a nullable overload is provided, and on purpose: for a non-nullable DateTimeOffset an
    // omitted value deserializes to MinValue/default, which is the "not supplied, use current
    // time" sentinel resolved by the ICreatableEntity hook on newly created entities — rejecting
    // it there would break omission. Use this only where null already expresses "not supplied"
    // (so MinValue/MaxValue can only be sent explicitly) AND the value is applied without the
    // creation-time fallback (e.g. updating an existing entity).
    public static IRuleBuilderOptions<T, DateTimeOffset?> IsFinite<T>(this IRuleBuilder<T, DateTimeOffset?> ruleBuilder)
    {
        return ruleBuilder
            .Must(x => x is null || (x.Value != DateTimeOffset.MinValue && x.Value != DateTimeOffset.MaxValue))
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
