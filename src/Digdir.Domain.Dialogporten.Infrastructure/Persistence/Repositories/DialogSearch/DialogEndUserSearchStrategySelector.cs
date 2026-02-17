using Digdir.Domain.Dialogporten.Application;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

// Selects the most suitable end-user dialog search strategy based on a score.
// This keeps the repository logic simple while allowing strategies to evolve independently.
internal sealed class DialogEndUserSearchStrategySelector(
    IOptionsSnapshot<ApplicationSettings> applicationSettings,
    IEnumerable<IQueryStrategy<EndUserSearchContext>> strategies) : ISearchStrategySelector<EndUserSearchContext>
{
    private const string DefaultStrategyName = PartyDrivenQueryStrategy.StrategyName;
    private readonly IReadOnlyList<IQueryStrategy<EndUserSearchContext>> _strategies = strategies.ToList();

    public IQueryStrategy<EndUserSearchContext> Select(EndUserSearchContext context)
    {
        // Feature flag controls whether we branch at all; otherwise stick to the default strategy.
        if (!applicationSettings.Value.FeatureToggle.UseBranchingLogicForDialogSearch)
        {
            return GetDefaultStrategy();
        }

        // Highest score wins; ties are stable by name.
        return _strategies
            .Select(strategy => (Strategy: strategy, Score: strategy.Score(context)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Strategy.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Strategy)
            .First();
    }

    private IQueryStrategy<EndUserSearchContext> GetDefaultStrategy()
    {
        var fallback = _strategies
            .FirstOrDefault(strategy => string.Equals(
                strategy.Name,
                DefaultStrategyName,
                StringComparison.OrdinalIgnoreCase));

        return fallback ?? throw new InvalidOperationException(
            $"Missing default end-user search strategy '{DefaultStrategyName}'.");
    }
}
