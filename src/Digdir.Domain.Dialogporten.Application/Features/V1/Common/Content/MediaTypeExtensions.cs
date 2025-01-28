using Digdir.Domain.Dialogporten.Domain;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

internal static class MediaTypeExtensions
{
#pragma warning disable CS0618 // Type or member is obsolete
    public static string ConvertIfDeprecatedMediaType(this string mediaType)
    {
        return mediaType switch
        {
            MediaTypes.EmbeddableMarkdownDeprecated => MediaTypes.EmbeddableMarkdown,
            MediaTypes.LegacyEmbeddableHtmlDeprecated => MediaTypes.LegacyEmbeddableHtml,
            _ => mediaType
        };
    }
#pragma warning restore CS0618 // Type or member is obsolete
}
