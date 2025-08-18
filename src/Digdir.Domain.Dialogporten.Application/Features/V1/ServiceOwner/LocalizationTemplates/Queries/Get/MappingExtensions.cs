using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Queries.Get;

internal static class MappingExtensions
{
    public static List<GetLocalizationTemplateDto> ToDto(this LocalizationTemplateSet templateSet)
    {
        return templateSet.Templates
            .Select(template => new GetLocalizationTemplateDto
            {
                Org = templateSet.Org,
                Id = templateSet.Id,
                LanguageCode = template.LanguageCode,
                Template = template.Template
            }).ToList();
    }
}
