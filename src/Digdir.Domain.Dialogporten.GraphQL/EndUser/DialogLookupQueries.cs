using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup;
using MediatR;
using static Digdir.Domain.Dialogporten.GraphQL.Common.Constants;
using DialogLookupModel = Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogLookup.DialogLookup;
using GetDialogLookupQuery = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogLookup.Queries.Get.GetDialogLookupQuery;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser;

public sealed partial class Queries
{
    public async Task<DialogLookupPayload> GetDialogLookup(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [Argument] string instanceUrn,
        [GlobalState(AcceptLanguage)] AcceptedLanguages? acceptLanguage,
        CancellationToken cancellationToken)
    {
        var request = new GetDialogLookupQuery
        {
            InstanceUrn = instanceUrn,
            AcceptedLanguages = acceptLanguage?.AcceptedLanguage
        };

        var result = await mediator.Send(request, cancellationToken);

        return result.Match(
            success => new DialogLookupPayload
            {
                Lookup = mapper.Map<DialogLookupModel>(success)
            },
            notFound => new DialogLookupPayload
            {
                Errors = [new DialogLookupNotFound { Message = notFound.Message }]
            },
            forbidden => new DialogLookupPayload
            {
                Errors = forbidden.Reasons.Count > 0
                    ? [.. forbidden.Reasons.Select(x => new DialogLookupForbidden { Message = x })]
                    : [new DialogLookupForbidden()]
            },
            validationError => new DialogLookupPayload
            {
                Errors = [.. validationError.Errors.Select(x => new DialogLookupValidationError { Message = x.ErrorMessage })]
            });
    }
}
