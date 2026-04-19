using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories.DialogSearch.EndUser.Sql;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public sealed class DialogFreeTextSearchSqlHelpersTests
{
    [Theory]
    [InlineData("melding betaling", "melding OR betaling")]
    [InlineData("melding   betaling", "melding OR betaling")]
    [InlineData("\"oppsummeringstekst inneholde\" melding", "\"oppsummeringstekst inneholde\" OR melding")]
    public void CreateFreeTextSearchQuery_ShouldUseImplicitOr_ForPlainWhitespaceSeparatedTerms(
        string search,
        string expectedSearchString)
    {
        var query = CreateQuery(search);

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);

        Assert.Equal(expectedSearchString, ftsQuery.SearchString);
    }

    [Theory]
    [InlineData("melding AND betaling", "melding betaling")]
    [InlineData("melding and betaling", "melding betaling")]
    [InlineData("\"oppsummeringstekst inneholde\" AND melding", "\"oppsummeringstekst inneholde\" melding")]
    [InlineData("melding AND betaling OR skatt", "melding betaling OR skatt")]
    [InlineData("melding OR betaling", "melding OR betaling")]
    [InlineData("melding or betaling", "melding or betaling")]
    public void CreateFreeTextSearchQuery_ShouldPreserveExplicitBooleanOperators(
        string search,
        string expectedSearchString)
    {
        var query = CreateQuery(search);

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);

        Assert.Equal(expectedSearchString, ftsQuery.SearchString);
    }

    private static GetDialogsQuery CreateQuery(string search) =>
        new()
        {
            Deleted = false,
            Search = search
        };
}
