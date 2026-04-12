using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;

internal interface IQueryStrategy<in TContext>
{
    string Name { get; }
    int Score(TContext context);
    PostgresFormattableStringBuilder BuildSql(TContext context);
}
