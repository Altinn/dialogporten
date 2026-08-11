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
        context.UnauthorizedPresentation = source.UnauthorizedPresentation;
        return context;
    }

    internal static TContext? ToAuthorizationContext<TContext>(
        this ChildAuthorizationContextDto? source, TContext? destination = null)
        where TContext : AuthorizationContext, new()
    {
        if (source is null)
        {
            return null;
        }

        var context = destination ?? new TContext();
        context.ServiceResource = source.ServiceResource;
        context.AdditionalResourceAttribute = source.AdditionalResourceAttribute;
        context.Parties = [.. source.Parties];
        context.IncludeDialogParty = source.IncludeDialogParty;
        context.Action = null;
        context.UnauthorizedPresentation = source.UnauthorizedPresentation;
        return context;
    }
}
