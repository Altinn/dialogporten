using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

public sealed class AuthorizationContentValueDto : IContentValueDto
{
    public List<LocalizationDto> Value { get; set; } = [];

    public string MediaType { get; set; } = MediaTypes.PlainText;

    /// <summary>
    /// True if the authenticated user is authorized for this action. If not, the action will not be available
    /// and all endpoints will be replaced with a fixed placeholder.
    /// </summary>
    public bool IsAuthorized { get; set; }
}
