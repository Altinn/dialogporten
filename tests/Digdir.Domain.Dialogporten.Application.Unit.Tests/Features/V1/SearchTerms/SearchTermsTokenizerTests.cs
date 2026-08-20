using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.SearchTerms.Tokenizer;
using Xunit;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.SearchTerms;

public sealed class SearchTermsTokenizerTests
{
    private readonly SearchTermsTokenizer _sut = new();

    [Fact]
    public void Lowercases_And_Deduplicates_Tokens()
    {
        _sut.Tokenize("Skatt skatt SKATT").Should().BeEquivalentTo("skatt");
    }

    [Fact]
    public void Splits_On_Punctuation_And_Whitespace()
    {
        _sut.Tokenize("skattemelding, frist! (purring)")
            .Should().BeEquivalentTo("skattemelding", "frist", "purring");
    }

    [Fact]
    public void Keeps_Norwegian_Letters_Intact()
    {
        _sut.Tokenize("søknad blåbær ærlig").Should().BeEquivalentTo("søknad", "blåbær", "ærlig");
    }

    [Fact]
    public void Normalizes_Decomposed_Unicode_To_Composed_Form()
    {
        // 'å' as 'a' + combining ring (U+030A) must tokenize identically to the composed form.
        _sut.Tokenize("blåbær").Should().BeEquivalentTo("blåbær");
    }

    // Letter runs adjacent to digits are identifier fragments ('Hansen123' must not yield
    // 'hansen'), and nothing downstream re-checks for digit adjacency — the tokenizer is the
    // only place this class of PII/noise is rejected.
    [Theory]
    [InlineData("Hansen123")]
    [InlineData("123Hansen")]
    [InlineData("REF2024ABC")]
    [InlineData("covid19")]
    public void Rejects_Letter_Runs_Adjacent_To_Digits(string text)
    {
        _sut.Tokenize(text).Should().BeEmpty();
    }

    [Fact]
    public void Digit_Adjacency_Does_Not_Reject_Tokens_Beyond_A_Non_Alphanumeric_Boundary()
    {
        // 'A4' is rejected (letter adjoining digit), but the hyphen is a boundary, so 'skjema'
        // and the free-standing words survive.
        _sut.Tokenize("A4-skjema levert 2024").Should().BeEquivalentTo("skjema", "levert");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12 345")]
    public void Returns_Empty_Set_When_There_Is_Nothing_To_Tokenize(string text)
    {
        _sut.Tokenize(text).Should().BeEmpty();
    }
}
