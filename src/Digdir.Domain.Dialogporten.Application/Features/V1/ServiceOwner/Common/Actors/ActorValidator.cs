using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;

internal sealed class ActorValidator : AbstractValidator<ActorDto>
{

    public ActorValidator()
    {
        RuleFor(x => x.ActorType)
            .IsInEnum();

        RuleFor(x => x)
            .Must(dto => (dto.ActorId is null || dto.ActorName is null) &&
                         ((dto.ActorType == ActorType.Values.ServiceOwner && dto.ActorId is null && dto.ActorName is null) ||
                          (dto.ActorType != ActorType.Values.ServiceOwner && (dto.ActorId is not null || dto.ActorName is not null))))
            .WithMessage(ActorValidationErrorMessages.ActorIdActorNameExclusiveOr);

        RuleFor(x => x.ActorId!)
            .IsValidPartyIdentifier()
            .When(x => x.ActorId is not null);
    }
}
