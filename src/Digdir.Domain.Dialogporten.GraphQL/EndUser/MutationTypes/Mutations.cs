using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSystemLabels.Commands.Set;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSystemLabels.Commands.BulkSet;
using Digdir.Domain.Dialogporten.GraphQL.Common;
using MediatR;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;

public sealed class Mutations
{
    public async Task<SetSystemLabelPayload> SetSystemLabel(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        [Service] IHttpContextAccessor httpContextAccessor,
        SetSystemLabelInput input)
    {
        var command = mapper.Map<SetSystemLabelCommand>(input);
        var result = await mediator.Send(command);

        return result.Match(
            success =>
            {
                httpContextAccessor.HttpContext?.Response.Headers
                    .Append(Constants.ETag, success.Revision.ToString());

                return new SetSystemLabelPayload { Success = true };
            },
            entityNotFound => new SetSystemLabelPayload
            {
                Errors = [new SetSystemLabelEntityNotFound { Message = entityNotFound.Message }]
            },
            entityDeleted => new SetSystemLabelPayload
            {
                Errors = [new SetSystemLabelEntityDeleted { Message = entityDeleted.Message }]
            },
            validationError => new SetSystemLabelPayload
            {
                Errors = validationError.Errors.Select(x => new SetSystemLabelValidationError
                {
                    Message = x.ErrorMessage
                }).Cast<ISetSystemLabelError>().ToList()
            },
            domainError => new SetSystemLabelPayload
            {
                Errors = domainError.Errors.Select(x => new SetSystemLabelDomainError { Message = x.ErrorMessage })
                    .Cast<ISetSystemLabelError>().ToList()
            },
            concurrencyError => new SetSystemLabelPayload { Errors = [] });
    }

    public async Task<BulkSetSystemLabelPayload> BulkSetSystemLabels(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        BulkSetSystemLabelInput input)
    {
        var command = mapper.Map<BulkSetSystemLabelCommand>(input);
        var result = await mediator.Send(command);

        return result.Match(
            _ => new BulkSetSystemLabelPayload { Success = true },
            forbidden => new BulkSetSystemLabelPayload
            {
                Errors = [new BulkSetSystemLabelForbidden { Message = string.Join("; ", forbidden.Reasons) }]
            },
            domainError => new BulkSetSystemLabelPayload
            {
                Errors = domainError.Errors
                    .Select(x => new BulkSetSystemLabelDomainError { Message = x.ErrorMessage })
                    .Cast<IBulkSetSystemLabelError>()
                    .ToList()
            },
            validationError => new BulkSetSystemLabelPayload
            {
                Errors = validationError.Errors
                    .Select(x => new BulkSetSystemLabelValidationError { Message = x.ErrorMessage })
                    .Cast<IBulkSetSystemLabelError>()
                    .ToList()
            },
            concurrencyError => new BulkSetSystemLabelPayload { Errors = [] });
    }
}
