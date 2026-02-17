namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal interface IQueryStrategy<in TContext>
{
    string Name { get; }
    int Score(TContext context);
    PostgresFormattableStringBuilder BuildSql(TContext context);
}
