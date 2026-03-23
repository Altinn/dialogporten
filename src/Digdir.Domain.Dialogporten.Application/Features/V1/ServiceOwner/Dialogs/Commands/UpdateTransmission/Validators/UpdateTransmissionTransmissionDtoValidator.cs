using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission.Validators;

internal sealed class UpdateTransmissionTransmissionDtoValidator : AbstractValidator<UpdateTransmissionDto>
{
    public UpdateTransmissionTransmissionDtoValidator(
        IValidator<ActorDto> actorValidator,
        IValidator<TransmissionContentDto?> contentValidator,
        IValidator<TransmissionAttachmentDto> attachmentValidator,
        IValidator<TransmissionNavigationalActionDto> navigationalActionValidator)
    {
        // CreatedAt is not validated for InPast,
        // Dialog.VisibleFrom could have set the transmission
        // CreatedAt to a date in the future.

        RuleFor(x => x.IdempotentKey)
            .MinimumLength(Constants.MinIdempotentKeyLength)
            .MaximumLength(Constants.MaxIdempotentKeyLength);

        RuleFor(x => x.ExtendedType)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength)
            .When(x => x.ExtendedType is not null);

        RuleFor(x => x.ExternalReference)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.Sender)
            .NotNull()
            .SetValidator(actorValidator);

        RuleFor(x => x.AuthorizationAttribute)
            .IsValidAuthorizationAttribute();

        RuleFor(x => x.Attachments)
            .UniqueBy(x => x.Id);

        RuleForEach(x => x.Attachments)
            .SetValidator(attachmentValidator);

        RuleForEach(x => x.NavigationalActions)
            .SetValidator(navigationalActionValidator);

        RuleFor(x => x.Content)
            .NotEmpty()
            .SetValidator(contentValidator);
    }
}
