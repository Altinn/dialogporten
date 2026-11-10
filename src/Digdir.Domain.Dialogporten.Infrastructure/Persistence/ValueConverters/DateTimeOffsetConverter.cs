using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Digdir.Domain.Dialogporten.Domain.Common.Exceptions;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.ValueConverters;

internal sealed class DateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    // Npgsql maps DateTimeOffset.MaxValue/MinValue to the PostgreSQL special timestamp values
    // 'infinity'/'-infinity', which break downstream date arithmetic (e.g. the search index sort
    // key casts EXTRACT(EPOCH ...) to bigint and fails on infinity). Clamp one second inside the
    // extremes so a finite timestamp is always stored, regardless of which write path produced it.
    private static readonly DateTimeOffset Min = DateTimeOffset.MinValue.AddSeconds(1);
    private static readonly DateTimeOffset Max = DateTimeOffset.MaxValue.AddSeconds(-1);

    public DateTimeOffsetConverter()
        : base(
            d => d.ToUniversalTime().Clamp(Min, Max),
            d => d.ToUniversalTime())
    {
    }
}
