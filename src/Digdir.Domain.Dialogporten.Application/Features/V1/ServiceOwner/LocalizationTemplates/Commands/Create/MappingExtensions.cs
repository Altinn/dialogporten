using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Common;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Commands.Create;

internal static class MappingExtensions
{
    public static LocalizationTemplateSet ToEntity(this TemplateSetDto dto)
    {
        var templates = dto.Templates
            .Select(x => new LocalizationTemplate(x.LanguageCode, x.Template))
            .ToList();

        return new LocalizationTemplateSet(dto.Org!, dto.Id, templates);
    }
}
