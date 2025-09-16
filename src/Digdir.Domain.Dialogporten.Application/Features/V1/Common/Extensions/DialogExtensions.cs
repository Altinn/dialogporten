using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;

internal static class DialogExtensions
{
    public static void FilterLocalizations(this DialogEntity dialog, List<AcceptedLanguage>? acceptedLanguages)
    {
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

    private static void PruneLocalizations(this List<Localization>? localizations, List<AcceptedLanguage>? acceptedLanguages)
    {
        if (localizations is null || acceptedLanguages is null)
        {
            return;
        }
        var orderByDescending = acceptedLanguages.OrderByDescending(x => x.Weight);
        Localization? localization = null;
        foreach (var acceptedLanguage in orderByDescending)
        {
            localization = localizations.FirstOrDefault(x => x.LanguageCode == acceptedLanguage.LanguageCode);
            if (localization is not null)
            {
                localizations.Clear();
                localizations.Add(localization);
                return;
            }
        }

        // Fallbacks
        if (acceptedLanguages.Select(x => x.LanguageCode).Any(x => x is "sv" or "da"))
        {
            localization ??= localizations.FirstOrDefault(x => x.LanguageCode == "nb");
        }

        localization ??= localizations.FirstOrDefault(x => x.LanguageCode == "en");
        localization ??= localizations.FirstOrDefault(x => x.LanguageCode == "nb");

        if (localization is null)
        {
            return;
        }

        localizations.Clear();
        localizations.Add(localization);
    }
}
