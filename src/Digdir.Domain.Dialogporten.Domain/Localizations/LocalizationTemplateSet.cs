using Digdir.Library.Entity.Abstractions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Domain.Dialogporten.Domain.Localizations;

public class LocalizationTemplateSet : IJoinEntity
{
    public string Org { get; }
    public string Id { get; }
    public string? ImmutableCopy { get; private set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<LocalizationTemplate> LocalizationTemplates { get; set; } = [];

    public LocalizationTemplateSet(string org, string id)
    {
        Org = org ?? throw new ArgumentNullException(nameof(org));
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public LocalizationTemplateSet CreateImmutableCopy()
    {
        ImmutableCopy = $"{Id}$fmjesiojfioj";
        var cpy = new LocalizationTemplateSet(Org, ImmutableCopy);
        return cpy;
    }


}

public class LocalizationTemplate : IIdentifiableEntity
{
    public Guid Id { get; set; }
    public string Template { get; private set; }
    public string LanguageCode { get; }

    public LocalizationTemplateSet TemplateSet { get; set; }

    public LocalizationTemplate(string template, string languageCode)
    {
        Template = template ?? throw new ArgumentNullException(nameof(template));
        LanguageCode = Localization.IsValidCultureCode(languageCode)
            ? Localization.NormalizeCultureCode(languageCode)
            : throw new ArgumentException($"'{languageCode}' is an invalid language code.", nameof(languageCode));
    }
}
