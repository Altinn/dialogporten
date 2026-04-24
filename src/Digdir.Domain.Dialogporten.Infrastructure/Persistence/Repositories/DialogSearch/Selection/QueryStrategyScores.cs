namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.Selection;

internal static class QueryStrategyScores
{
    public const int Ineligible = 0;
    public const int Eligible = 100;
    public const int Preferred = 1000;
    public const int HighlyPreferred = 2000;
}
