using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;

namespace Digdir.Domain.Dialogporten.Application.Common.Authorization;

/// <summary>
/// Collects the entity references for the dialog token's authorized entities claim
/// (<see cref="DialogTokenClaimTypes.AuthorizedEntities"/>): for every authorization-context-carrying entity the
/// user is authorized for, the service owner supplied token reference when the context has one, otherwise the
/// carrying entity's id. Entities sharing a token reference form an OR-group because an authorized member adds
/// the shared value and duplicate values collapse in the token. Entities without a context are governed by the
/// action claim and never appear here.
/// </summary>
public static class AuthorizedContextReferences
{
    extension(List<string> references)
    {
        public void AddIfAuthorized<TCarrier>(TCarrier carrier, bool isAuthorized)
            where TCarrier : IAuthorizationContextCarrier, IIdentifiableEntity
        {
            if (isAuthorized && carrier.AuthorizationContext is { } context)
            {
                references.Add(context.TokenReference ?? carrier.Id.ToString());
            }
        }
    }
}
