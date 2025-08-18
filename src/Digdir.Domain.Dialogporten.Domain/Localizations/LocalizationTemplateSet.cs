using System.Collections.ObjectModel;
using Digdir.Library.Entity.Abstractions.Features.Aggregate;
using Digdir.Library.Entity.Abstractions.Features.Versionable;

namespace Digdir.Domain.Dialogporten.Domain.Localizations;

public class LocalizationTemplateSet : IVersionableEntity
{
    public const string ImmutableCopyIdSeparator = "$";

    private readonly List<LocalizationTemplate> _templates;

    public string Org { get; }
    public string Id { get; }

    public string? ImmutableCopyId { get; private set; }
    public LocalizationTemplateSet? ImmutableCopy { get; private set; }
    public LocalizationTemplateSet? Source { get; private set; }

    public Guid Revision { get; set; }

    [AggregateChild]
    public ReadOnlyCollection<LocalizationTemplate> Templates => _templates.AsReadOnly();

    public LocalizationTemplateSet(string org, string id, List<LocalizationTemplate> templates)
    {
        _templates = templates ?? throw new ArgumentNullException(nameof(templates));
        Org = org ?? throw new ArgumentNullException(nameof(org));
        Id = id ?? throw new ArgumentNullException(nameof(id));
    }

    public LocalizationTemplateSet GetOrCreateImmutableCopy()
    {
        if (IsImmutableCopy())
        {
            throw new InvalidOperationException("Cannot create an immutable copy of an immutable copy.");
        }

        if (ImmutableCopyId is not null && ImmutableCopy is null)
        {
            throw new InvalidOperationException($"Immutable copy with ID '{ImmutableCopyId}' is not loaded.");
        }

        if (ImmutableCopy is not null)
        {
            return ImmutableCopy;
        }

        ImmutableCopyId ??= $"{Id}${Guid.NewGuid().ToString("N")[..12]}";
        return ImmutableCopy = new LocalizationTemplateSet(Org, ImmutableCopyId, Templates.ToList())
        {
            Source = this
        };
    }

    public void AddOrUpdateTemplate(string languageCode, string template)
    {
        ImmutableCopy = null;
        ImmutableCopyId = null;
        languageCode = Localization.NormalizeCultureCodeStrict(languageCode);
        var templateEntity = _templates.FirstOrDefault(t => t.LanguageCode == languageCode);
        if (templateEntity is not null)
        {
            templateEntity.UpdateTemplate(template);
            return;
        }

        _templates.Add(new LocalizationTemplate(languageCode, template));
    }

    public void RemoveTemplate(string languageCode)
    {
        ImmutableCopy = null;
        ImmutableCopyId = null;
        languageCode = Localization.NormalizeCultureCodeStrict(languageCode);
        _templates.RemoveAll(t => t.LanguageCode == languageCode);
    }

    public bool IsImmutableCopy() => Id.Contains(ImmutableCopyIdSeparator);
}

public class LocalizationTemplate
{
    public string LanguageCode { get; private set; }
    public string Template { get; private set; }

    public LocalizationTemplate(string languageCode, string template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        LanguageCode = Localization.NormalizeCultureCodeStrict(languageCode);
        Template = template;
    }

    public void UpdateTemplate(string template)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(template);
        Template = template;
    }
}
