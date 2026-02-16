using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

public interface IContentValueDto
{
    /// <summary>
    /// A list of localizations for the content.
    /// </summary>
    List<LocalizationDto> Value { get; set; }

    /// <summary>
    /// Media type of the content, this can also indicate that the content is embeddable.
    /// </summary>
    string MediaType { get; set; }
}
