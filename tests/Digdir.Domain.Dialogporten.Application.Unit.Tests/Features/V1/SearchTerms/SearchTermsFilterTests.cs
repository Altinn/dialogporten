using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Filtering;
using Xunit;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.SearchTerms;

public sealed class SearchTermsFilterTests
{
    private readonly SearchTermsFilter _sut = new();

    [Fact]
    public void Keeps_Regular_Words_Meeting_The_Minimum_Length()
    {
        _sut.ShouldKeep("skattemelding", minLength: 5).Should().BeTrue();
    }

    [Fact]
    public void Rejects_Words_Shorter_Than_The_Minimum_Length()
    {
        _sut.ShouldKeep("fire", minLength: 5).Should().BeFalse();
    }

    [Theory]
    [InlineData("altinn")] // bundled no.txt
    [InlineData("about")] // bundled en.txt — the exact-match stoplist is the union of all lists
    public void Rejects_Bundled_Stopwords(string stopword)
    {
        _sut.ShouldKeep(stopword, minLength: 1).Should().BeFalse();
    }

    [Fact]
    public void StopwordsForLanguage_Maps_Norwegian_Codes_To_The_Norwegian_List()
    {
        foreach (var language in new[] { "nb", "nn", "no" })
        {
            var stopwords = _sut.StopwordsForLanguage(language);
            stopwords.Should().Contain("altinn");
            // Only the language's own stopwords may be stem-matched; see SearchTermsFilter for why.
            stopwords.Should().NotContain("about");
        }
    }

    [Fact]
    public void StopwordsForLanguage_Maps_English_To_The_English_List()
    {
        _sut.StopwordsForLanguage("en").Should().Contain("about");
    }

    [Fact]
    public void StopwordsForLanguage_Returns_Empty_For_Unknown_Languages()
    {
        _sut.StopwordsForLanguage("de").Should().BeEmpty();
    }
}
