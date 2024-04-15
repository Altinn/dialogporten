using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.Common.Authorization;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialog;
using HotChocolate.Authorization;
using MediatR;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

[Authorize(Policy = AuthorizationPolicy.EndUser)]
public class DialogQueries : ISearchDialogQuery, IDialogByIdQuery
{
    public async Task<Dialog> GetDialogById(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [Argument] Guid dialogId,
        CancellationToken cancellationToken)
    {
        var request = new GetDialogQuery { DialogId = dialogId };
        var result = await mediator.Send(request, cancellationToken);
        var getDialogResult = result.Match(
            dialog => dialog,
            notFound => throw new NotImplementedException("Not found"),
            deleted => throw new NotImplementedException("Deleted"),
            forbidden => throw new NotImplementedException("Forbidden"));

        var dialog = mapper.Map<Dialog>(getDialogResult);

        return dialog;
    }

    public async Task<DialogSearch> SearchDialog(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [Argument] Guid dialogId,
        CancellationToken cancellationToken)
    {
        var request = new GetDialogQuery { DialogId = dialogId };
        var result = await mediator.Send(request, cancellationToken);
        var dialogSearchResult = result.Match(
            dialog => dialog,
            notFound => throw new NotImplementedException("Not found"),
            deleted => throw new NotImplementedException("Deleted"),
            forbidden => throw new NotImplementedException("Forbidden"));

        var dialog = mapper.Map<DialogSearch>(dialogSearchResult);

        return dialog;
    }
}
