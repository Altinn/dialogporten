namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

// Selects the most suitable end-user dialog search strategy based on a score.
// This keeps the repository logic simple while allowing strategies to evolve independently.
internal sealed class DialogEndUserSearchStrategySelector(
    IEnumerable<IQueryStrategy<EndUserSearchContext>> strategies) : ISearchStrategySelector<EndUserSearchContext>
{
    private readonly IReadOnlyList<IQueryStrategy<EndUserSearchContext>> _strategies = strategies.ToList();

    public IQueryStrategy<EndUserSearchContext> Select(EndUserSearchContext context) =>
        _strategies
            .Select(strategy => (Strategy: strategy, Score: strategy.Score(context)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Strategy.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Strategy)
            .First();
}
