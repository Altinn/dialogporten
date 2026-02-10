using Digdir.Domain.Dialogporten.Application;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch;

internal interface IDialogEndUserSearchStrategySelector
{
    IDialogEndUserSearchStrategy Select(EndUserSearchContext context);
}

// Selects the most suitable end-user dialog search strategy based on a score.
// This keeps the repository logic simple while allowing strategies to evolve independently.
internal sealed class DialogEndUserSearchStrategySelector(
    IOptionsSnapshot<ApplicationSettings> applicationSettings,
    IEnumerable<IDialogEndUserSearchStrategy> strategies) : IDialogEndUserSearchStrategySelector
{
    private const string DefaultStrategyName = "PartyDriven";
    private readonly IReadOnlyList<IDialogEndUserSearchStrategy> _strategies = strategies.ToList();

    public IDialogEndUserSearchStrategy Select(EndUserSearchContext context)
    {
        // Feature flag controls whether we branch at all; otherwise stick to the default strategy.
        if (!applicationSettings.Value.FeatureToggle.UseBranchingLogicForDialogSearch)
        {
            var fallback = GetDefaultStrategy();
            fallback.SetContext(context);
            return fallback;
        }

        // Highest positive score wins; ties are stable by name.
        var selected = _strategies
            .Select(strategy => (Strategy: strategy, Score: strategy.Score(context)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Strategy.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()
            .Strategy;

        var resolved = selected ?? GetDefaultStrategy();
        resolved.SetContext(context);
        return resolved;
    }

    private IDialogEndUserSearchStrategy GetDefaultStrategy()
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
