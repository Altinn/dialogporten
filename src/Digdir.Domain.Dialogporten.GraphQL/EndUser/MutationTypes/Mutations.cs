using AutoMapper;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogLabels.Commands.Set;
using MediatR;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;

public sealed class Mutations
{
    public async Task<SetSystemLabelPayload> SetSystemLabel(
        [Service] ISender mediator,
        [Service] IMapper mapper,
        SetSystemLabelInput input)
    {
        var command = mapper.Map<SetDialogLabelCommand>(input);
        var result = await mediator.Send(command);

        return result.Match(
            success => new SetSystemLabelPayload { Success = true },
            entityNotFound => new SetSystemLabelPayload
            {
                Errors = [new SetSystemLabelEntityNotFound { Message = entityNotFound.Message }]
            },
            forbidden => new SetSystemLabelPayload
            {
                Errors = forbidden.Reasons.Select(x => new SetSystemLabelForbidden { Message = x })
                    .Cast<ISetSystemLabelError>().ToList()
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
}
