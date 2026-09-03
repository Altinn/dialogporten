#pragma warning disable CS0618 // Obsolete legacy authorization fields are validated for backwards compatibility
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;
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
        IValidator<TransmissionNavigationalActionDto> navigationalActionValidator,
        IValidator<AuthorizationContextDto> authorizationContextValidator)
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

        RuleFor(x => x.AuthorizationAttribute)
            .Null()
            .WithMessage($"'{{PropertyName}}' cannot be combined with '{nameof(UpdateTransmissionDto.AuthorizationContext)}'.")
            .When(x => x.AuthorizationContext is not null);

        RuleFor(x => x.AuthorizationContext)
            .SetValidator(authorizationContextValidator!)
            .When(x => x.AuthorizationContext is not null);

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
