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
    [InlineData("melding AND betaling skatt", "melding betaling OR skatt")]
    [InlineData("melding OR betaling skatt", "melding OR betaling OR skatt")]
    [InlineData("melding skatt OR betaling", "melding OR skatt OR betaling")]
    [InlineData("\"oppsummeringstekst inneholde\" OR melding betaling", "\"oppsummeringstekst inneholde\" OR melding OR betaling")]
    public void CreateFreeTextSearchQuery_ShouldPreserveExplicitBooleanOperators(
        string search,
        string expectedSearchString)
    {
        var query = CreateQuery(search);

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);

        Assert.Equal(expectedSearchString, ftsQuery.SearchString);
    }

    [Theory]
    [InlineData("invoice -paid", "invoice -paid")]
    [InlineData("invoice   -paid", "invoice -paid")]
    [InlineData("invoice AND -paid", "invoice -paid")]
    [InlineData("invoice OR -paid", "invoice OR -paid")]
    [InlineData("\"payment reminder\" -paid", "\"payment reminder\" -paid")]
    public void CreateFreeTextSearchQuery_ShouldPreserveImplicitAnd_WhenNegatedTermsArePresent(
        string search,
        string expectedSearchString)
    {
        var query = CreateQuery(search);

        var ftsQuery = DialogFreeTextSearchSqlHelpers.CreateFreeTextSearchQuery(query);

        Assert.Equal(expectedSearchString, ftsQuery.SearchString);
    }

    [Theory]
    [InlineData("   ", "   ")]
    [InlineData("AND", "")]
    [InlineData("and", "")]
    [InlineData("OR", "OR")]
    [InlineData("or", "or")]
    [InlineData("the", "the")]
    public void CreateFreeTextSearchQuery_ShouldReturnExpectedString_ForEmptyOrOperatorOnlyInputs(
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
