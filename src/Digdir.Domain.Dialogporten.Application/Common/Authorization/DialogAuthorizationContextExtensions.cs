using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

internal static class DialogAuthorizationContextExtensions
{
    extension(DialogEntity dialog)
    {
        /// <summary>
        /// Every authorization context in the dialog aggregate, across all carrier kinds.
        /// </summary>
        internal IEnumerable<AuthorizationContext> GetAuthorizationContexts() =>
            Enumerable.Empty<AuthorizationContext?>()
                .Concat(dialog.ApiActions.Select(x => x.AuthorizationContext))
                .Concat(dialog.GuiActions.Select(x => x.AuthorizationContext))
                .Concat(dialog.Transmissions.Select(x => x.AuthorizationContext))
                .Concat(dialog.Attachments.Select(x => x.AuthorizationContext))
                .Concat(dialog.Transmissions.SelectMany(x => x.Attachments).Select(x => x.AuthorizationContext))
                .Concat(dialog.Transmissions.SelectMany(x => x.NavigationalActions).Select(x => x.AuthorizationContext))
                .OfType<AuthorizationContext>();

        /// <summary>
        /// Validation of authorization contexts that requires the dialog itself, and therefore cannot be done by
        /// the stateless DTO validators: "parties" cannot contain the dialog's own party (dialog party checks
        /// express that, and an unrestricted entry there would silently cancel any restricted check), and a client
        /// delegation cannot be made through the dialog's own party.
        /// </summary>
        internal IEnumerable<DomainFailure> ValidateAuthorizationContexts()
        {
            var contexts = dialog.GetAuthorizationContexts().ToList();

            var dialogPartyInParties = contexts
                .SelectMany(x => x.Parties)
                .Where(dialog.IsDialogParty)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(party => new DomainFailure(
                    $"{nameof(AuthorizationContext)}.{nameof(AuthorizationContext.Parties)}",
                    $"'{nameof(AuthorizationContext.Parties)}' cannot contain the dialog's own party ('{party}'); use a " +
                    $"'{nameof(AuthorizationContext.DialogPartyChecks)}' entry with '{nameof(DialogPartyCheck.AccessRestriction)}' " +
                    $"'{DialogPartyCheckAccessRestriction.Values.None}' instead."));

            var dialogPartyAsViaParty = contexts
                .SelectMany(x => x.DialogPartyChecks)
                .Select(x => x.ViaParty)
                .OfType<string>()
                .Where(dialog.IsDialogParty)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(viaParty => new DomainFailure(
                    $"{nameof(AuthorizationContext)}.{nameof(AuthorizationContext.DialogPartyChecks)}",
                    $"'{nameof(DialogPartyCheck.ViaParty)}' cannot be the dialog's own party ('{viaParty}')."));

            return dialogPartyInParties.Concat(dialogPartyAsViaParty);
        }

        // Case-insensitive, as the dialog's party is lower-cased on create while context values are stored as given.
        private bool IsDialogParty(string party) =>
            string.Equals(party, dialog.Party, StringComparison.OrdinalIgnoreCase);
    }
}
