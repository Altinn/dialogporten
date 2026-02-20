using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Conventions;

internal static class ReplaceIndexAnnotations
{
    public const string ReplacesIndex = "Dialogporten:ReplacesIndex";
}

internal static class IndexBuilderExtensions
{
    public static IndexBuilder ReplacesIndex(this IndexBuilder builder, string indexName)
        => builder.HasAnnotation(ReplaceIndexAnnotations.ReplacesIndex, indexName);
}
