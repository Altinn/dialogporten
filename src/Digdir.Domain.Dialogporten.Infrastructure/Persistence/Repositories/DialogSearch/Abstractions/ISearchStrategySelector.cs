namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;

internal interface ISearchStrategySelector<in TContext>
{
    IQueryStrategy<TContext> Select(TContext context);
}
