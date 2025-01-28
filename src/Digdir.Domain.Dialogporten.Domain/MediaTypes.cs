namespace Digdir.Domain.Dialogporten.Domain;

public static class MediaTypes
{
    public const string EmbeddablePrefix = "application/vnd.dialogporten.frontchannelembed";

    [Obsolete($"Use {nameof(EmbeddableMarkdown)} instead")]
    public const string EmbeddableMarkdownDeprecated = $"{EmbeddablePrefix}+json;type=markdown";
    public const string EmbeddableMarkdown = $"{EmbeddablePrefix}-url;type=text/markdown";

    [Obsolete($"Use {nameof(LegacyEmbeddableHtml)} instead")]
    public const string LegacyEmbeddableHtmlDeprecated = $"{EmbeddablePrefix}+json;type=html";
    public const string LegacyEmbeddableHtml = $"{EmbeddablePrefix}-url;type=text/html";

    public const string LegacyHtml = "text/html";
    public const string Markdown = "text/markdown";
    public const string PlainText = "text/plain";
}
