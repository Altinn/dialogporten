using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;

public static class ContentValueDtoExtensions
{
    public static void ReplaceUnauthorizedContentReference(this ContentValueDto? contentReference)
    {
        if (contentReference is null)
        {
            return;
        }

        contentReference.Value = contentReference.Value
            .Select(localization => new LocalizationDto
            {
                LanguageCode = localization.LanguageCode,
                Value = Constants.UnauthorizedUri.ToString()
            })
            .ToList();
    }
}
