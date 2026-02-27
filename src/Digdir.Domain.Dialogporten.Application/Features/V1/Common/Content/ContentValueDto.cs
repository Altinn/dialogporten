using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

public interface IContentValueDto
{
    List<LocalizationDto> Value { get; set; }
    string MediaType { get; set; }
}

public sealed class ContentValueDto : IContentValueDto
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

public sealed class AuthorizationContentValueDto : IContentValueDto
{
    /// <summary>
    /// A list of localizations for the content.
    /// </summary>
    public List<LocalizationDto> Value { get; set; } = [];

    /// <summary>
    /// Media type of the content, this can also indicate that the content is embeddable.
    /// </summary>
    public string MediaType { get; set; } = MediaTypes.PlainText;

    /// <summary>
    /// True if the authenticated user is authorized for this content. If not, the endpoints will 
    /// be replaced with a fixed placeholder.
    /// </summary>
    public bool IsAuthorized { get; set; }
}
