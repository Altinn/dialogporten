using System.Buffers;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Text;
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

public static class Template
{
    private const string PlaceholderStart = "{{";
    private const string PlaceholderEnd = "}}";
    private const int MinPlaceholderKeyLength = 3;
    private const int MaxPlaceholderKeyLength = 100;
    private const int MaxPlaceholderValueLength = 1000;

    private static readonly SearchValues<string> PlaceholderTags = SearchValues.Create(
        new ReadOnlySpan<string>([PlaceholderStart, PlaceholderEnd]),
        StringComparison.OrdinalIgnoreCase);

    private static readonly FrozenSet<char> ValidPlaceholderKeys = Enumerable
        .Range('a', 26).Select(c => (char)c)
        .Concat(Enumerable.Range('A', 26).Select(c => (char)c))
        .Concat(Enumerable.Range('0', 10).Select(c => (char)c))
        .Concat("_-.æøåÆØÅ")
        .ToFrozenSet();

    public static string Render(ReadOnlySpan<char> template, Dictionary<string, string> parameters)
    {
        var sb = new StringBuilder();
        var quickParamLookup = parameters.GetAlternateLookup<ReadOnlySpan<char>>();

        while (TryGetNextPlaceholderRange(template, out var before, out var placeholder, out var rest))
        {
            sb.Append(template[before]);
            var placeholderKey = template[placeholder].Trim();
            if (quickParamLookup.TryGetValue(placeholderKey, out var value)) sb.Append(value);
            template = template[rest];
        }

        sb.Append(template);
        return sb.ToString();
    }

    public static bool IsValid(ReadOnlySpan<char> template)
    {
        while (TryGetNextPlaceholderRange(template, out var before, out var placeholder, out var rest))
        {
            if (template[before].ContainsAny(PlaceholderTags)) return false;
            if (!IsValidPlaceholderKey(template[placeholder])) return false;
            template = template[rest];
        }

        return !template.ContainsAny(PlaceholderTags);
    }

    public static bool IsValidParameters(Dictionary<string, string> parameters)
    {
        foreach (var (key, value) in parameters)
        {
            if (!IsValidPlaceholderKey(key) || !IsValidPlaceholderValue(value))
                return false;
        }

        return true;
    }

    private static bool IsValidPlaceholderKey(ReadOnlySpan<char> placeholderKey)
    {
        var trimmedPlaceholder = placeholderKey.Trim();
        if (trimmedPlaceholder.Length is < MinPlaceholderKeyLength or > MaxPlaceholderKeyLength) return false;
        foreach (var placeholderKeyChar in placeholderKey.Trim())
        {
            if (!ValidPlaceholderKeys.Contains(placeholderKeyChar))
                return false;
        }

        return true;
    }

    private static bool IsValidPlaceholderValue(ReadOnlySpan<char> placeholderValue) =>
        placeholderValue.Length <= MaxPlaceholderValueLength &&
        !placeholderValue.ContainsAny(PlaceholderTags);

    private static bool TryGetNextPlaceholderRange(ReadOnlySpan<char> template, out Range before, out Range placeholder, out Range rest)
    {
        const int notFound = -1;
        before = placeholder = rest = Range.All;

        var placeholderStart = template.IndexOf(PlaceholderStart);
        if (placeholderStart is notFound) return false;
        var placeholderEnd = template[placeholderStart..].IndexOf(PlaceholderEnd);
        if (placeholderEnd is notFound) return false;

        placeholder = new Range(placeholderStart + PlaceholderStart.Length, placeholderStart + placeholderEnd);
        before = new Range(Index.Start, placeholderStart);
        rest = new Range(placeholder.End.Value + PlaceholderEnd.Length, Index.End);

        return true;
    }
}
