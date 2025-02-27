namespace Digdir.Domain.Dialogporten.Domain;

public static class MediaTypes
{
    public const string EmbeddablePrefix = "application/vnd.dialogporten.frontchannelembed";

    public const string EmbeddableMarkdown = $"{EmbeddablePrefix}-url;type=text/markdown";
    public const string EmbeddableMarkdownDeprecated = $"{EmbeddablePrefix}+json;type=markdown";

    public const string LegacyEmbeddableHtml = $"{EmbeddablePrefix}-url;type=text/html";
    public const string LegacyEmbeddableHtmlDeprecated = $"{EmbeddablePrefix}+json;type=html";

    public const string LegacyHtml = "text/html";
    public const string Markdown = "text/markdown";
    public const string PlainText = "text/plain";
}

// Temporary mapping for deprecated media types,
// we will support both old and new media types
// until correspondence is updated and deployed
// TODO: https://github.com/Altinn/dialogporten/issues/1782
public static class MediaTypeExtensions
{
    public static string MapDeprecatedMediaType(this string mediaType)
        => mediaType switch
        {
            MediaTypes.EmbeddableMarkdownDeprecated => MediaTypes.EmbeddableMarkdown,
            MediaTypes.LegacyEmbeddableHtmlDeprecated => MediaTypes.LegacyEmbeddableHtml,
            _ => mediaType
        };
}
