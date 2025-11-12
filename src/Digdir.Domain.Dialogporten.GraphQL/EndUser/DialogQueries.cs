using AppAny.HotChocolate.FluentValidation;
using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;
using MediatR;
using static Digdir.Domain.Dialogporten.GraphQL.Common.Constants;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public partial class Queries
{
    public async Task<DialogByIdPayload> GetDialogById(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [Service] IHttpContextAccessor httpContextAccessor,
        [Argument] Guid dialogId,
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        CancellationToken cancellationToken)
    {
        var request = new GetDialogQuery
        {
            DialogId = dialogId,
            AcceptedLanguages = acceptLanguage?.AcceptedLanguage
        };

        var result = await mediator.Send(request, cancellationToken);
        return result.Match(
            dialog => new DialogByIdPayload { Dialog = mapper.Map<Dialog>(dialog) },
            notFound => new DialogByIdPayload { Errors = [new DialogByIdNotFound { Message = notFound.Message }] },
            notVisible =>
            {
                if (httpContextAccessor.HttpContext != null)
                {
                    httpContextAccessor.HttpContext.Response.Headers.Expires = notVisible.VisibleFrom.ToString("R");
                }

                return new DialogByIdPayload
                {
                    Errors =
                    [
                        new DialogByIdNotVisible()
                        {
                            Message = notVisible.Message,
                            VisibleFrom = notVisible.VisibleFrom
                        }
                    ]
                };
            },
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
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        [UseFluentValidation, UseValidator<SearchDialogInputValidator>] SearchDialogInput input,
        CancellationToken cancellationToken)
    {
        var searchDialogQuery = mapper.Map<SearchDialogQuery>(input);
        searchDialogQuery.AcceptedLanguages = acceptLanguage?.AcceptedLanguage;

        if (!ContinuationTokenSet<SearchDialogQueryOrderDefinition, IntermediateDialogDto>.TryParse(
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

        if (orderSet is not null)
        {
            searchDialogQuery.OrderBy = orderSet;
        }

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
