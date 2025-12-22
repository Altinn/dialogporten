using Digdir.Domain.Dialogporten.GraphQL.Common.Authorization;
using HotChocolate.Authorization;
using Constants = Digdir.Domain.Dialogporten.Infrastructure.GraphQL.GraphQlSubscriptionConstants;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable once EntityNameCapturedOnly.Global
#pragma warning disable IDE0060

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

public sealed class Subscriptions
{
    [Subscribe]
    [Authorize(AuthorizationPolicy.EndUserSubscription, ApplyPolicy.Validation)]
    [GraphQLDescription("Requires a dialog token in the 'Authorization' header.")]
    [Topic($"{Constants.DialogEventsTopic}{{{nameof(dialogId)}}}")]
    public DialogEventPayload DialogEvents(Guid dialogId,
        [EventMessage] DialogEventPayload eventMessage) => eventMessage;
}
