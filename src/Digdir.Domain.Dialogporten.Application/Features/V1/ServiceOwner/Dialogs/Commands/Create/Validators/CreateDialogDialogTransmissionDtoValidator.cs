using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogDialogTransmissionDtoValidator : AbstractValidator<TransmissionDto>
{
    public CreateDialogDialogTransmissionDtoValidator(
        IValidator<ActorDto> actorValidator,
        IValidator<TransmissionContentDto?> contentValidator,
        IValidator<TransmissionAttachmentDto> attachmentValidator)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast();

        RuleFor(x => x.CreatedAt)
            .IsInPast();

        RuleFor(x => x.ExtendedType)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength)
            .When(x => x.ExtendedType is not null);

        RuleFor(x => x.ExternalReference)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.RelatedTransmissionId)
            .NotEqual(x => x.Id)
            .WithMessage(x => $"A transmission cannot reference itself ({nameof(x.RelatedTransmissionId)} is equal to {nameof(x.Id)}, '{x.Id}').")
            .When(x => x.RelatedTransmissionId.HasValue);

        RuleFor(x => x.Sender)
            .NotNull()
            .SetValidator(actorValidator);

        RuleFor(x => x.Sender.ActorType)
            .Must(x => x == ActorType.Values.PartyRepresentative)
            .When(x => x.Type
                is DialogTransmissionType.Values.Submission
                or DialogTransmissionType.Values.Correction)
            .WithMessage(x => $"Sender actor type must be '{ActorType.Values.PartyRepresentative}' for transmission type '{x.Type}'.");

        RuleFor(x => x.Sender.ActorType)
            .Must(x => x == ActorType.Values.ServiceOwner)
            .When(x => x.Type
                is not DialogTransmissionType.Values.Submission
                and not DialogTransmissionType.Values.Correction)
            .WithMessage(x => $"Sender actor type must be '{ActorType.Values.ServiceOwner}' for transmission type '{x.Type}'.");

        RuleFor(x => x.AuthorizationAttribute)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.Attachments)
            .UniqueBy(x => x.Id);

        RuleForEach(x => x.Attachments)
            .SetValidator(attachmentValidator);

        When(CreateDialogCommandValidator.IsApiOnly, () =>
                RuleFor(x => x.Content)
                    .SetValidator(contentValidator)
                    .When(x => x.Content is not null))
            .Otherwise(() =>
                RuleFor(x => x.Content)
                    .NotEmpty()
                    .SetValidator(contentValidator));
    }
}
