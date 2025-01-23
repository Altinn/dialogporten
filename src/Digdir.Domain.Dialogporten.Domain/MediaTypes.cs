namespace Digdir.Domain.Dialogporten.Domain;

public static class MediaTypes
{
    public const string EmbeddablePrefix = "application/vnd.dialogporten.frontchannelembed";
    public const string EmbeddableMarkdownDeprecated = $"{EmbeddablePrefix}+json;type=markdown";
    public const string LegacyEmbeddableHtmlDeprecated = $"{EmbeddablePrefix}+json;type=html";

    public const string LegacyHtml = "text/html";
    public const string Markdown = "text/markdown";
    public const string PlainText = "text/plain";
}
