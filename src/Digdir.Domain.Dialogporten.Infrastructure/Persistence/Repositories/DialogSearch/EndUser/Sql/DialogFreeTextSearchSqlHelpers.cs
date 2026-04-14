using System.Text.RegularExpressions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;

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
            .Where(term => !term.Equals("OR", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return terms.Length == 0
            ? search
            : string.Join(" OR ", terms);
    }

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
