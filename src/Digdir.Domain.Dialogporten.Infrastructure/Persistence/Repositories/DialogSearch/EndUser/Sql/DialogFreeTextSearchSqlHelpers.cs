using System.Text.RegularExpressions;
using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;

internal static partial class DialogFreeTextSearchSqlHelpers
{
    internal static FreeTextSearchQuery CreateFreeTextSearchQuery(GetDialogsQuery query) =>
        new(
            TsConfigName: GetTsConfigName(query.SearchLanguageCode),
            SearchString: BuildOrSearchString(query.Search!));

    // Builds a guarded FTS predicate. numnode/querytree reject empty or stop-word-only
    // queries before the @@ operator is evaluated, so callers do not accidentally match
    // every row or force an expensive scan for a query PostgreSQL cannot index.
    internal static PostgresFormattableStringBuilder BuildFreeTextSearchPredicate(FreeTextSearchQuery ftsQuery) =>
        new PostgresFormattableStringBuilder()
            .Append(
                $"""
                numnode(websearch_to_tsquery({ftsQuery.TsConfigName}::regconfig, {ftsQuery.SearchString}::text)) > 0
                AND querytree(websearch_to_tsquery({ftsQuery.TsConfigName}::regconfig, {ftsQuery.SearchString}::text)) <> 'T'
                AND ds."SearchVector" @@ websearch_to_tsquery({ftsQuery.TsConfigName}::regconfig, {ftsQuery.SearchString}::text)
                """);

    internal static int GetCandidateLimitPerParty(
        int effectivePartyCount,
        int candidateCap,
        int candidateMinimumPerParty)
    {
        if (effectivePartyCount <= 0)
        {
            return candidateMinimumPerParty;
        }

        var fairShare = (int)Math.Ceiling((double)candidateCap / effectivePartyCount);
        return Math.Max(candidateMinimumPerParty, fairShare);
    }

    private static string BuildOrSearchString(string search)
    {
        var terms = SearchTermRegex()
            .Matches(search)
            .Select(match => match.Value)
            .ToArray();

        if (terms.Length == 0)
        {
            return search;
        }

        var implicitSeparator = terms.Any(IsNegatedTerm) ? " " : " OR ";
        var searchStringBuilder = new System.Text.StringBuilder();
        var hasWrittenOperand = false;
        string? pendingOperator = null;

        foreach (var term in terms)
        {
            if (IsExplicitAndOperator(term) || IsExplicitOrOperator(term))
            {
                pendingOperator = term;
                continue;
            }

            if (hasWrittenOperand)
            {
                searchStringBuilder.Append(pendingOperator switch
                {
                    null => implicitSeparator,
                    var explicitOperator when IsExplicitOrOperator(explicitOperator) => $" {explicitOperator} ",
                    _ => " "
                });
            }

            searchStringBuilder.Append(term);
            hasWrittenOperand = true;
            pendingOperator = null;
        }

        if (hasWrittenOperand)
        {
            return searchStringBuilder.ToString();
        }

        return string.Join(" ", terms.Where(IsExplicitOrOperator));
    }

    private static bool IsExplicitAndOperator(string term) =>
        term.Equals("AND", StringComparison.OrdinalIgnoreCase);

    private static bool IsExplicitOrOperator(string term) =>
        term.Equals("OR", StringComparison.OrdinalIgnoreCase);

    private static bool IsNegatedTerm(string term) =>
        term.StartsWith('-');

    private static string GetTsConfigName(string? languageCode) => languageCode switch
    {
        "ar" => "arabic",
        "hy" => "armenian",
        "eu" => "basque",
        "ca" => "catalan",
        "da" => "danish",
        "nl" => "dutch",
        "en" => "english",
        "fi" => "finnish",
        "fr" => "french",
        "de" => "german",
        "el" => "greek",
        "hi" => "hindi",
        "hu" => "hungarian",
        "id" => "indonesian",
        "ga" => "irish",
        "it" => "italian",
        "lt" => "lithuanian",
        "ne" => "nepali",
        "nb" or "nn" or "no" => "norwegian",
        "pt" => "portuguese",
        "ro" => "romanian",
        "ru" => "russian",
        "sr" => "serbian",
        "es" => "spanish",
        "sv" => "swedish",
        "ta" => "tamil",
        "tr" => "turkish",
        "yi" => "yiddish",
        _ => "simple"
    };

    [GeneratedRegex("""(?:"[^"]+"|\S+)""", RegexOptions.CultureInvariant)]
    private static partial Regex SearchTermRegex();
}

internal sealed record FreeTextSearchQuery(string TsConfigName, string SearchString);
