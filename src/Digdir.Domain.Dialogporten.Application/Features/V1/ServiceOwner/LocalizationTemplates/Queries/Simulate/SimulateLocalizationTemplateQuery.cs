using System.Collections.ObjectModel;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using MediatR;
using OneOf;
using Scriban;
using Scriban.Parsing;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.LocalizationTemplates.Queries.Simulate;

public class SimulateLocalizationTemplateQuery : IRequest<SimulateLocalizationTemplateResult>
{
    public string? Org { get; set; }
    public required string Id { get; init; }
    public required ReadOnlyDictionary<string, string> Parameters { get; init; }
}

[GenerateOneOf]
public sealed partial class SimulateLocalizationTemplateResult : OneOfBase<List<LocalizationDto>, EntityNotFound, ValidationError>;

internal sealed class SimulateLocalizationTemplateQueryHandler : IRequestHandler<SimulateLocalizationTemplateQuery, SimulateLocalizationTemplateResult>
{
    private readonly IDialogDbContext _db;
    private readonly IUserOrganizationRegistry _userOrganizationRegistry;

    public SimulateLocalizationTemplateQueryHandler(
        IDialogDbContext db,
        IUserOrganizationRegistry userOrganizationRegistry)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _userOrganizationRegistry = userOrganizationRegistry ?? throw new ArgumentNullException(nameof(userOrganizationRegistry));
    }

    public async Task<SimulateLocalizationTemplateResult> Handle(SimulateLocalizationTemplateQuery request, CancellationToken cancellationToken)
    {
        request.Org ??= await _userOrganizationRegistry.GetCurrentUserOrgShortNameStrict(cancellationToken);

        var templateSet = await _db.LocalizationTemplateSets
            .FindAsync([request.Id, request.Org], cancellationToken);

        if (templateSet is null)
        {
            return new EntityNotFound<LocalizationTemplateSet>([request.Id, request.Org]);
        }


    }
}

internal sealed class LocalizationTemplateSetResolver
{
    public static List<LocalizationDto> Resolve(
        LocalizationTemplateSet templateSet,
        ReadOnlyDictionary<string, string> parameters) =>
        templateSet.Templates
            .Select(template => ResolveTemplate(template, parameters))
            .ToList();

    private static LocalizationDto ResolveTemplate(LocalizationTemplate template, ReadOnlyDictionary<string, string> parameters)
    {
        var t = Template.Parse(template.Template);

        var trimmedParameters = parameters
            .Select(x => x.Key.Split('$', 2, StringSplitOptions.TrimEntries) switch
            {
                [var key, var languageCode]
                    when languageCode.Equals(template.LanguageCode, StringComparison.OrdinalIgnoreCase) =>
                    (key, languageCode, value: x.Value),
                [var key] =>
                    (key, languageCode: null, value: x.Value),
                _ =>
                    throw new ArgumentException($"Invalid parameter key format: {x.Key}")
            })
            .GroupBy(x => x.key)
            .ToDictionary(x => x.Key, group => group.FirstOrDefault(x => x.languageCode is not null, group.First()).value);

        var result = t.Render(trimmedParameters);
        return new()
        {
            LanguageCode = template.LanguageCode,
            Value = result
        };
    }

    public static Dictionary<string, string> FilterParameters(
        string targetLang,
        Dictionary<string, string> parameters)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in parameters)
        {
            var split = key.Split('$', 2, StringSplitOptions.TrimEntries)
            var dollarIndex = key.IndexOf('$');
            string baseKey;
            string? langCode = null;

            if (dollarIndex >= 0)
            {
                baseKey = key.Substring(0, dollarIndex);
                langCode = key.Substring(dollarIndex + 1);
            }
            else
            {
                baseKey = key;
            }

            // Case 1: Exact language match → always wins
            if (langCode != null &&
                langCode.Equals(targetLang, StringComparison.OrdinalIgnoreCase))
            {
                result[baseKey] = value;
            }
            // Case 2: Generic key → use only if no language-specific match has been added yet
            else if (langCode == null && !result.ContainsKey(baseKey))
            {
                result[baseKey] = value;
            }
        }

        return result;
    }
}
