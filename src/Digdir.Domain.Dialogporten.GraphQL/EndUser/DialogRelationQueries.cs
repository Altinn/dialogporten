using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchRelations;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogRelations;
using MediatR;
using static Digdir.Domain.Dialogporten.GraphQL.Common.Constants;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public sealed partial class Queries
{
    [GraphQLDescription("""
        Gets the dialogs related to a dialog (its parent, children and related dialogs), as a subset of the search
        list items. Related dialogs the user cannot access are left out silently. Ordered by the related dialog's id,
        which follows creation order.
        """)]
    public async Task<DialogRelationsPayload> GetDialogRelations(
        [Service] ISender mediator,
        [Argument] Guid dialogId,
        [Argument] DialogRelationsInput? input,
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        CancellationToken cancellationToken)
    {
        var request = new SearchDialogRelationsQuery
        {
            DialogId = dialogId,
            Type = input?.Type?.ToDomain(),
            Party = input?.Party,
            ServiceResource = input?.ServiceResource,
            Status = input?.Status?.Select(x => x.ToDomain()).ToList(),
            ExcludeApiOnly = input?.ExcludeApiOnly,
            Limit = input?.Limit,
            ContinuationToken = input?.ContinuationToken,
            AcceptedLanguages = acceptLanguage?.AcceptedLanguage
        };

        var result = await mediator.Send(request, cancellationToken);
        return result.Match(
            page => page.ToPayload(),
            notFound => new DialogRelationsPayload { Errors = [new DialogRelationsNotFound { Message = notFound.Message }] },
            validationError => new DialogRelationsPayload
            {
                Errors = [.. validationError.Errors.Select(x => new DialogRelationsValidationError { Message = x.ErrorMessage })]
            });
    }
}
