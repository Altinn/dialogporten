namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal interface ISearchStrategySelector<in TContext>
{
    IQueryStrategy<TContext> Select(TContext context);
}
