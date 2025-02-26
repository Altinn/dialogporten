using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

public sealed class ContentValueDto
{
    /// <summary>
    /// A list of localizations for the content.
    /// </summary>
    public List<LocalizationDto> Value { get; set; } = [];

    /// <summary>
    /// Media type of the content, this can also indicate that the content is embeddable.
    /// </summary>
    public string MediaType { get; set; } = MediaTypes.PlainText;
}
