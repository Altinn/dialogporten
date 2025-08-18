namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Queries.Get;

public sealed class GetLocalizationTemplateDto
{
    public required string Org { get; init; }
    public required string Id { get; init; }
    public required string LanguageCode { get; init; }
    public required string Template { get; init; }
}
