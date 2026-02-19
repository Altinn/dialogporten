using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Conventions;

internal sealed class ReplacesIndexConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var index in entityType.GetIndexes().ToList())
            {
                var replacedName = index.FindAnnotation(ReplaceIndexAnnotations.ReplacesIndex)?.Value as string;
                if (string.IsNullOrWhiteSpace(replacedName))
                {
                    continue;
                }

                var toRemove = entityType.GetIndexes()
                    .FirstOrDefault(candidate =>
                        string.Equals(candidate.GetDatabaseName(), replacedName, StringComparison.Ordinal));

                if (toRemove is not null)
                {
                    entityType.RemoveIndex(toRemove);
                }
            }
        }
    }
}
