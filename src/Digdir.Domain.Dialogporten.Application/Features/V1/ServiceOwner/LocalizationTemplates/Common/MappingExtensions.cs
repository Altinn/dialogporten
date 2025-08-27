using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Common;

internal static class MappingExtensions
{
    public static LocalizationTemplateSetDto ToDto(this LocalizationTemplateSet templateSet)
    {
        return new LocalizationTemplateSetDto
        {
            Id = templateSet.Id,
            Org = templateSet.Org,
            Templates = templateSet.Templates
                .Select(template => new LocalizationTemplateDto
                {
                    LanguageCode = template.LanguageCode,
                    Template = template.Template
                }).ToList().AsReadOnly()
        };
    }

    public static LocalizationTemplateSet ToEntity(this LocalizationTemplateSetDto dto)
    {
        var templates = dto.Templates
            .Select(x => new LocalizationTemplate(x.LanguageCode, x.Template))
            .ToList();

        return new LocalizationTemplateSet(dto.Org!, dto.Id, templates);
    }
}
