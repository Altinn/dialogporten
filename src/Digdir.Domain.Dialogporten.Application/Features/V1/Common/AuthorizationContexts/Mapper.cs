using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;

internal static class AuthorizationContextMapExtensions
{
    internal static TContext? ToAuthorizationContext<TContext>(
        this AuthorizationContextDto? source, TContext? destination = null)
        where TContext : AuthorizationContext, new()
    {
        if (source is null)
        {
            return null;
        }

        // Copy into the existing context when present to avoid delete+insert churn on every update.
        var context = destination ?? new TContext();
        context.ServiceResource = source.ServiceResource;
        context.AdditionalResourceAttribute = source.AdditionalResourceAttribute;
        context.Parties = [.. source.Parties];
        context.IncludeDialogParty = source.IncludeDialogParty;
        context.Action = source.Action;
        context.TokenReference = source.TokenRef;
        context.UnauthorizedPresentation = source.UnauthorizedPresentation;
        return context;
    }
}

internal static class LegacyAuthorizationFieldMapExtensions
{
    extension(string? action)
    {
        /// <summary>
        /// The legacy action in the form it is persisted in: the empty-string sentinel when the caller
        /// supplied none, because the entity carries an authorization context instead. See
        /// <see cref="DialogGuiAction.Action"/> for why the column stays NOT NULL.
        ///
        /// Whitespace normalizes to the sentinel too. The rule that the legacy action must be empty when a
        /// context is supplied is expressed with FluentValidation's Empty(), which counts a whitespace-only
        /// string as empty — so storing it verbatim would leave a value that
        /// <see cref="DialogGuiAction.EffectiveLegacyAction"/> reports as a real legacy action.
        /// </summary>
        internal string ToStoredLegacyAction() =>
            string.IsNullOrWhiteSpace(action) ? string.Empty : action;
    }

    extension(string? authorizationAttribute)
    {
        /// <summary>
        /// The legacy authorization attribute in the form it is persisted in on a transmission. A
        /// transmission governed by an authorization context stores
        /// <see cref="Constants.ExcludedTransmissionAttribute"/>, which is inert here — access is decided
        /// from the context — but keeps the transmission hidden from code predating this feature.
        /// The two are mutually exclusive on the write surface, so nothing is overwritten.
        /// </summary>
        internal string? ToStoredTransmissionAttribute(AuthorizationContextDto? authorizationContext) =>
            authorizationContext is null
                ? authorizationAttribute
                : Constants.ExcludedTransmissionAttribute;
    }
}
