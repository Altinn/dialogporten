using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Common;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;

// The authorizationContext members these maps carry are flagged experimental for consumers; the SDK's
// own mapping layer is part of the feature, so it opts in rather than warning about itself.
#pragma warning disable DPEXP001

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

/// <summary>
/// Maps the authorization context carried by attachments, transmissions and actions between the read model
/// (<see cref="AuthorizationContext"/>) and the write model (<see cref="AuthorizationContextInput"/>). Unlike
/// the shared localization collections elsewhere in this layer, <c>Parties</c> is copied rather than reused by
/// reference: callers routinely edit the parties of a context they have just read, and aliasing the list would
/// mutate the source model.
/// </summary>
internal static class AuthorizationContextMappingExtensions
{
    internal static AuthorizationContextInput ToAuthorizationContextInput(this AuthorizationContext source) => new()
    {
        ServiceResource = source.ServiceResource,
        AdditionalResourceAttribute = source.AdditionalResourceAttribute,
        Parties = [.. source.Parties],
        IncludeDialogParty = source.IncludeDialogParty,
        Action = source.Action,
        TokenRef = source.TokenRef,
        UnauthorizedPresentation = source.UnauthorizedPresentation,
    };

    internal static AuthorizationContext ToAuthorizationContext(this AuthorizationContextInput source) => new()
    {
        ServiceResource = source.ServiceResource,
        AdditionalResourceAttribute = source.AdditionalResourceAttribute,
        Parties = [.. source.Parties],
        IncludeDialogParty = source.IncludeDialogParty,
        Action = source.Action,
        TokenRef = source.TokenRef,
        UnauthorizedPresentation = source.UnauthorizedPresentation,
    };
}

#pragma warning restore DPEXP001
