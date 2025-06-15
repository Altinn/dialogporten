using AppAny.HotChocolate.FluentValidation;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using MediatR;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    public async Task<DialogByIdPayload> GetDialogById(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [Argument] Guid dialogId,
        CancellationToken cancellationToken)
    {
        var request = new GetDialogQuery { DialogId = dialogId };
        var result = await mediator.Send(request, cancellationToken);
        return result.Match(
            dialog => new DialogByIdPayload { Dialog = mapper.Map<Dialog>(dialog) },
            notFound => new DialogByIdPayload { Errors = [new DialogByIdNotFound { Message = notFound.Message }] },
            deleted => new DialogByIdPayload { Errors = [new DialogByIdDeleted { Message = deleted.Message }] },
            forbidden =>
            {
                var response = new DialogByIdPayload();

                if (forbidden.Reasons.Any(x => x.Contains(Constants.AltinnAuthLevelTooLow)))
                {
                    response.Errors.Add(new DialogByIdForbiddenAuthLevelTooLow());
                }
                else
                {
                    response.Errors.Add(new DialogByIdForbidden());
                }

                return response;
            });
    }

    public async Task<SearchDialogsPayload> SearchDialogs(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [UseFluentValidation, UseValidator<SearchDialogInputValidator>]
        SearchDialogInput input,
        CancellationToken cancellationToken)
    {
        var searchDialogQuery = mapper.Map<SearchDialogQuery>(input);

        if (!ContinuationTokenSet<SearchDialogQueryOrderDefinition, DialogEntity>.TryParse(
                input.ContinuationToken, out var continuationTokenSet) && input.ContinuationToken != null)
        {
            return new SearchDialogsPayload
            {
                Errors = [new SearchDialogContinuationTokenParsingError()]
            };
        }

        searchDialogQuery.ContinuationToken = continuationTokenSet;

        if (!input.OrderBy.TryToOrderSet(out var orderSet) && input.OrderBy != null)
        {
            return new SearchDialogsPayload
            {
                Errors = [new SearchDialogOrderByParsingError()]
            };
        }

        searchDialogQuery.OrderBy = orderSet;

        var result = await mediator.Send(searchDialogQuery, cancellationToken);

        return result.Match(
            paginatedList =>
            {
                var mappedResult = mapper.Map<SearchDialogsPayload>(paginatedList);
                mappedResult.OrderBy = paginatedList.OrderBy.AsSpan().ToSearchDialogSortTypeList();
                return mappedResult;
            },
            validationError => new SearchDialogsPayload
            {
                Errors = [.. validationError.Errors.Select(x => new SearchDialogValidationError { Message = x.ErrorMessage })]
            },
            forbidden => new SearchDialogsPayload { Errors = [new SearchDialogForbidden()] });
    }
}
