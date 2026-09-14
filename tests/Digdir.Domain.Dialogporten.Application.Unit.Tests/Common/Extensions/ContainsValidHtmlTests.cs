using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common.Extensions;

/// <summary>
/// Guards the HTML allowlist applied to localized dialog content. Anchors may carry
/// exactly one attribute, a href on https; every other allowed tag must carry none.
/// </summary>
public sealed class ContainsValidHtmlTests
{
    private sealed class Validator : AbstractValidator<LocalizationDto>
    {
        public Validator()
        {
            RuleFor(x => x).ContainsValidHtml();
        }
    }

    private static bool IsValid(string html) =>
        new Validator().Validate(new LocalizationDto { Value = html, LanguageCode = "nb" }).IsValid;

    [Theory]
    [InlineData("plain text")]
    [InlineData("<p>paragraph</p>")]
    [InlineData("<ul><li>one</li><li>two</li></ul>")]
    [InlineData("<table><thead><tr><th>h</th></tr></thead><tbody><tr><td>c</td></tr></tbody></table>")]
    [InlineData("<a href=\"https://example.com\">link</a>")]
    [InlineData("<a href=\"HTTPS://EXAMPLE.COM\">uppercase scheme</a>")]
    public void Accepts_AllowedMarkup(string html) =>
        IsValid(html).Should().BeTrue();

    [Theory]
    [InlineData("<script>alert(1)</script>", "disallowed tag")]
    [InlineData("<div>not allowed</div>", "disallowed tag")]
    [InlineData("<p class=\"x\">attribute on non-anchor</p>", "attributes only allowed on anchors")]
    [InlineData("<a href=\"http://example.com\">plain http</a>", "href must be https")]
    [InlineData("<a href=\"javascript:alert(1)\">script scheme</a>", "href must be https")]
    [InlineData("<a href=\"https://example.com\" target=\"_blank\">extra attribute</a>", "anchors allow exactly one attribute")]
    [InlineData("<a name=\"anchor\">no href</a>", "the single attribute must be href")]
    public void Rejects_DisallowedMarkup(string html, string because) =>
        IsValid(html).Should().BeFalse(because);

    [Fact]
    public void Rejects_AnchorWithValuelessHref()
    {
        // HtmlAgilityPack surfaces a valueless attribute with an empty value rather than
        // null, so this must fail on the https check rather than throw.
        IsValid("<a href>valueless</a>").Should().BeFalse();
    }
}
