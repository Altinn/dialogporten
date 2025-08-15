using System.Text.RegularExpressions;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;
using HtmlAgilityPack;

namespace Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;

internal static partial class FluentValidationStringExtensions
{
    private static readonly string[] AllowedTags = ["p", "a", "br", "em", "strong", "ul", "ol", "li"];
    private static readonly string ContainsValidHtmlError =
        "Value contains unsupported HTML. The following tags are supported: " +
        $"[{string.Join(",", AllowedTags.Select(x => '<' + x + '>'))}]. Tag attributes " +
        "are not supported except for on '<a>' which must contain a 'href' starting " +
        "with 'https://'.";

    public static IRuleBuilderOptions<T, string?> IsValidUri<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(uri => uri is null || Uri.IsWellFormedUriString(uri, UriKind.RelativeOrAbsolute))
            .WithMessage("'{PropertyName}' is not a well-formatted URI.");
    }

    public static IRuleBuilderOptions<T, string?> IsValidHttpsUrl<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(x => x is null || (Uri.TryCreate(x, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("'{PropertyName}' is not a well-formatted HTTPS URL.");
    }

    public static IRuleBuilderOptions<T, Uri?> IsValidHttpsUrl<T>(this IRuleBuilder<T, Uri?> ruleBuilder)
    {
        return ruleBuilder
            .Must(x => x is null || (x.IsAbsoluteUri && x.Scheme == Uri.UriSchemeHttps))
            .WithMessage("'{PropertyName}' is not a well-formatted HTTPS URL.");
    }

    public static IRuleBuilderOptions<T, string?> ContainsValidHtml<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(x => x is null || x.HtmlAgilityPackCheck())
            .WithMessage(ContainsValidHtmlError);
    }

    [GeneratedRegex(
        "^(?:urn:altinn:(?:[a-z][a-z0-9_-]*):)?[a-z][a-z0-9_-]*$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.NonBacktracking)]
    private static partial Regex ValidAuthorizationAttributeRegex();

    public static IRuleBuilderOptions<T, string?> IsValidAuthorizationAttribute<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(Constants.DefaultMaxStringLength)
            .Must(value => value is null || ValidAuthorizationAttributeRegex().IsMatch(value))
            .WithMessage("'{PropertyName}' must be on format 'urn:altinn:{resourcetype}:{resourcename}' or " +
                         "{resourcename} with valid names.");
    }

    private static bool HtmlAgilityPackCheck(this string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var nodes = doc.DocumentNode.DescendantsAndSelf();
        foreach (var node in nodes)
        {
            if (node.NodeType != HtmlNodeType.Element) continue;

            if (!AllowedTags.Contains(node.Name))
            {
                return false;
            }
            // If the node is a hyperlink, it should only have a href attribute,
            // and it must start with 'https://'
            if (node.IsAnchorTag())
            {
                if (!node.IsValidAnchorTag())
                {
                    return false;
                }

                continue;
            }

            if (node.Attributes.Count > 0)
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsAnchorTag(this HtmlNode node)
    {
        const string anchorTag = "a";
        return node.Name == anchorTag;
    }

    private static bool IsValidAnchorTag(this HtmlNode node)
    {
        const string https = "https://";
        const string href = "href";
        return node.Attributes.Count == 1 &&
            node.Attributes[href] is not null &&
            node.Attributes[href].Value.StartsWith(https, StringComparison.InvariantCultureIgnoreCase);
    }
}
