using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;

internal static class DialogExtensions
{
    public static void FilterLocalizations(this IntermediateDialogDto dialog, List<AcceptedLanguage>? acceptedLanguages)
    {
        if (acceptedLanguages is null)
        {
            return;
        }

        foreach (var dialogContent in dialog.Content)
        {
            dialogContent.Value.Localizations.PruneLocalizations(acceptedLanguages);
        }
    }

    public static void FilterLocalizations(this DialogEntity dialog, List<AcceptedLanguage>? acceptedLanguages)
    {
        if (acceptedLanguages is null)
        {
            return;
        }

        foreach (var dialogContent in dialog.Content)
        {
            dialogContent.Value.Localizations.PruneLocalizations(acceptedLanguages);
        }

        foreach (var attachment in dialog.Attachments)
        {
            attachment.DisplayName?.Localizations.PruneLocalizations(acceptedLanguages);
        }

        foreach (var guiAction in dialog.GuiActions)
        {
            guiAction.Title?.Localizations.PruneLocalizations(acceptedLanguages);
            guiAction.Prompt?.Localizations.PruneLocalizations(acceptedLanguages);
        }

        foreach (var transmission in dialog.Transmissions)
        {
            transmission.FilterLocalizations(acceptedLanguages);
        }

        foreach (var activity in dialog.Activities)
        {
            activity.FilterLocalizations(acceptedLanguages);
        }
    }

    public static void FilterLocalizations(this DialogTransmission transmission, List<AcceptedLanguage>? acceptedLanguages)
    {
        if (acceptedLanguages is null)
        {
            return;
        }

        foreach (var dialogTransmissionContent in transmission.Content)
        {
            dialogTransmissionContent.Value.Localizations.PruneLocalizations(acceptedLanguages);
        }

        foreach (var attachment in transmission.Attachments)
        {
            attachment.DisplayName?.Localizations.PruneLocalizations(acceptedLanguages);
        }
    }

    public static void FilterLocalizations(this DialogActivity activity, List<AcceptedLanguage>? acceptedLanguages) =>
            activity.Description?.Localizations.PruneLocalizations(acceptedLanguages);

    public static void PruneLocalizations(this List<Localization>? localizations, List<AcceptedLanguage>? acceptedLanguages)
    {
        if (localizations is null || acceptedLanguages is null) return;

        var danishOrSwedish = acceptedLanguages
            .Select(x => x.LanguageCode)
            .Any(x => x is "sv" or "da");

        var preferredLanguage = acceptedLanguages
            .OrderByDescending(l => l.Weight)
            .Select(x => x.LanguageCode)
            .Concat(danishOrSwedish ? ["nb", "en"] : ["en", "nb"])
            .Distinct() // order is preserved, keep first occurrence
            .Join(localizations, x => x, x => x.LanguageCode, (_, y) => y) // Order is preserved from outer side
            .FirstOrDefault();

        if (preferredLanguage is null) return;

        localizations.Clear();
        localizations.Add(preferredLanguage);
    }

    public static void PruneLocalizations(this List<LocalizationDto>? localizations, List<AcceptedLanguage>? acceptedLanguages)
    {
        if (localizations is null || acceptedLanguages is null) return;

        var danishOrSwedish = acceptedLanguages
            .Select(x => x.LanguageCode)
            .Any(x => x is "sv" or "da");

        var preferredLanguage = acceptedLanguages
            .OrderByDescending(l => l.Weight)
            .Select(x => x.LanguageCode)
            .Concat(danishOrSwedish ? ["nb", "en"] : ["en", "nb"])
            .Distinct() // order is preserved, keep first occurrence
            .Join(localizations, x => x, x => x.LanguageCode, (_, y) => y) // Order is preserved from outer side
            .FirstOrDefault();

        if (preferredLanguage is null) return;

        localizations.Clear();
        localizations.Add(preferredLanguage);
    }
}
