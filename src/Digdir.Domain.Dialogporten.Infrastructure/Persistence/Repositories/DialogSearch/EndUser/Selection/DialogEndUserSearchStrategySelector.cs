using System.Text.Json;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Selection;

// Selects the most suitable end-user dialog search strategy based on a score.
// This keeps the repository logic simple while allowing strategies to evolve independently.
internal sealed partial class DialogEndUserSearchStrategySelector(
    IEnumerable<IQueryStrategy<EndUserSearchContext>> strategies,
    ILogger<DialogEndUserSearchStrategySelector> logger) : ISearchStrategySelector<EndUserSearchContext>
{
    private readonly IReadOnlyList<IQueryStrategy<EndUserSearchContext>> _strategies = strategies.ToList();

    public IQueryStrategy<EndUserSearchContext> Select(EndUserSearchContext context)
    {
        var scoredStrategies = _strategies
            .Select(strategy => (Strategy: strategy, Score: strategy.Score(context)))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Strategy.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var selected = scoredStrategies[0];

        if (logger.IsEnabled(LogLevel.Information))
        {
            LogSelectionDiagnostics(context, scoredStrategies, selected);
        }

        return selected.Strategy;
    }

    private void LogSelectionDiagnostics(
        EndUserSearchContext context,
        (IQueryStrategy<EndUserSearchContext> Strategy, int Score)[] scoredStrategies,
        (IQueryStrategy<EndUserSearchContext> Strategy, int Score) selected)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var delegatedDialogCount = context.AuthorizedResources.DialogIds.Count;
        var runnerUpScore = scoredStrategies.Length > 1
            ? scoredStrategies[1].Score
            : selected.Score;
        var strategyScoresJson = JsonSerializer.Serialize(scoredStrategies.ToDictionary(
            x => x.Strategy.Name,
            x => x.Score,
            StringComparer.Ordinal));
        var effectivePartyCount = DialogEndUserSearchSqlHelpers.CountEffectiveParties(
            context.Query,
            context.AuthorizedResources);
        var hasScoreTie = scoredStrategies.Count(x => x.Score == selected.Score) > 1;

        LogQueryStrategyEndUser(
            logger,
            selected.Strategy.Name,
            strategyScoresJson,
            context.Query.Search is not null,
            delegatedDialogCount > 0,
            effectivePartyCount,
            context.Query.Party?.Count ?? 0,
            context.Query.ServiceResource?.Count ?? 0,
            delegatedDialogCount,
            selected.Score,
            selected.Score - runnerUpScore,
            hasScoreTie);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        SkipEnabledCheck = true,
        Message = "QueryStrategyEndUser: s={SelectedStrategy}, r={StrategyScoresJson}, fts={HasFreeTextSearch}, ind={HasInstanceDelegations}, pc={EffectivePartyCount}, qpc={QueryPartyCount}, qsc={QueryServiceResourceCount}, dc={DelegatedDialogCount}, ss={SelectedScore}, sm={ScoreMargin}, tie={HasScoreTie}")]
    private static partial void LogQueryStrategyEndUser(
        ILogger logger,
        string selectedStrategy,
        string strategyScoresJson,
        bool hasFreeTextSearch,
        bool hasInstanceDelegations,
        int effectivePartyCount,
        int queryPartyCount,
        int queryServiceResourceCount,
        int delegatedDialogCount,
        int selectedScore,
        int scoreMargin,
        bool hasScoreTie);
}
