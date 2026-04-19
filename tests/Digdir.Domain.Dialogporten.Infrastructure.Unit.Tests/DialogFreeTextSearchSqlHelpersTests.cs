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
    [InlineData("melding AND betaling")]
    [InlineData("melding and betaling")]
    [InlineData("melding OR betaling")]
    [InlineData("melding or betaling")]
    public void CreateFreeTextSearchQuery_ShouldPreserveExplicitBooleanOperators(string search)
    {
        var query = CreateQuery(search);

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);

        Assert.Equal(search, ftsQuery.SearchString);
    }

    private static GetDialogsQuery CreateQuery(string search) =>
        new()
        {
            Deleted = false,
            Search = search
        };
}
