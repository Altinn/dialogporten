using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogDialogAttachmentUrlDtoValidator : AbstractValidator<AttachmentUrlDto>
{
    public CreateDialogDialogAttachmentUrlDtoValidator(IClock clock)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast(clock);

        RuleFor(x => x.Url)
            .NotNull()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);

        RuleFor(x => x.ConsumerType)
            .IsInEnum();
    }
}
