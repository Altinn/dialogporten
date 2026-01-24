using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogDialogTransmissionDtoValidator : AbstractValidator<TransmissionDto>
{
    public UpdateDialogDialogTransmissionDtoValidator(
        IValidator<ActorDto> actorValidator,
        IValidator<TransmissionContentDto?> contentValidator,
        IValidator<TransmissionAttachmentDto> attachmentValidator,
        IClock clock)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast(clock);

        RuleFor(x => x.CreatedAt)
            .IsInPast(clock);

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

        RuleFor(x => x.AuthorizationAttribute)
            .IsValidAuthorizationAttribute();

        RuleFor(x => x.Attachments)
            .UniqueBy(x => x.Id);

        RuleForEach(x => x.Attachments)
            .SetValidator(attachmentValidator);

        When(UpdateDialogCommandValidator.IsApiOnly, () =>
                RuleFor(x => x.Content)
                    .SetValidator(contentValidator)
                    .When(x => x.Content is not null))
            .Otherwise(() =>
                RuleFor(x => x.Content)
                    .NotEmpty()
                    .SetValidator(contentValidator));
    }
}
