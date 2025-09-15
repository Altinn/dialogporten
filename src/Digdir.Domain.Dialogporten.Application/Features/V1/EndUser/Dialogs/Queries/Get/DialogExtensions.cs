using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;

internal static class DialogExtensions
{

    public static void FilterLocalizations(this DialogDto dialog, List<AcceptedLanguage>? acceptedLanguages)
    {
        if (acceptedLanguages is null || acceptedLanguages.Count == 0)
        {
            return;
        }

        // Content localizations

        dialog.Content.ExtendedStatus?.Value.PruneLocalizations(acceptedLanguages);
        dialog.Content.MainContentReference?.Value.PruneLocalizations(acceptedLanguages);
        dialog.Content.AdditionalInfo?.Value.PruneLocalizations(acceptedLanguages);
        dialog.Content.SenderName?.Value.PruneLocalizations(acceptedLanguages);
        dialog.Content.Summary?.Value.PruneLocalizations(acceptedLanguages);
        dialog.Content.Title.Value.PruneLocalizations(acceptedLanguages);

        // Attachment display name localizations
        foreach (var attachment in dialog.Attachments)
        {
            attachment.DisplayName.PruneLocalizations(acceptedLanguages);
        }

        // GUI Action localizations (Title and Prompt)
        foreach (var guiAction in dialog.GuiActions)
        {
            guiAction.Title.PruneLocalizations(acceptedLanguages);
            guiAction.Prompt?.PruneLocalizations(acceptedLanguages);
        }

        // Transmission localizations
        foreach (var transmission in dialog.Transmissions)
        {
            // Transmission content localizations
            transmission.Content.Summary?.Value.PruneLocalizations(acceptedLanguages);
            transmission.Content.Title.Value.PruneLocalizations(acceptedLanguages);
            transmission.Content.ContentReference?.Value.PruneLocalizations(acceptedLanguages);

            // Transmission attachment display name localizations
            foreach (var attachment in transmission.Attachments)
            {
                attachment.DisplayName.PruneLocalizations(acceptedLanguages);
            }
        }

        // Activity description localizations
        foreach (var activity in dialog.Activities)
        {
            activity.Description.PruneLocalizations(acceptedLanguages);
        }
    }


    private static void PruneLocalizations(this List<LocalizationDto> localizations, List<AcceptedLanguage> acceptedLanguages)
    {
        var orderByDescending = acceptedLanguages.OrderByDescending(x => x.Weight);
        LocalizationDto? localization = null;
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
        if (acceptedLanguages.Select(x => x.LanguageCode).Any(x => x is "sv" or "db"))
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
