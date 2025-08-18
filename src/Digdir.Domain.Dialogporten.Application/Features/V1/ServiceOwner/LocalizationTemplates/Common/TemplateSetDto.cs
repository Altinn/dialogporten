using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Common;

public sealed class TemplateSetDto
{
    public required string Id { get; init; }
    public string? Org { get; set; }
    public required ReadOnlyCollection<TemplateDto> Templates { get; init; }
}

public sealed class TemplateDto
{
    private readonly string _languageCode;
    public required string LanguageCode
    {
        get => _languageCode;
        [MemberNotNull(nameof(_languageCode))]
        init => _languageCode = Localization.NormalizeCultureCode(value);
    }

    public required string Template { get; init; }
}
