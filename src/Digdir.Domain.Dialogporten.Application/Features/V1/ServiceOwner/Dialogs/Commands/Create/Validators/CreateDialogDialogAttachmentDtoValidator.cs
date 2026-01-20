using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogDialogAttachmentDtoValidator : AbstractValidator<AttachmentDto>
{
    public CreateDialogDialogAttachmentDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<AttachmentUrlDto> urlValidator,
        IClock clock)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast(clock);

        RuleFor(x => x.DisplayName)
            .SetValidator(localizationsValidator);

        RuleFor(x => x.Urls)
            .NotEmpty()
            .ForEach(x => x.SetValidator(urlValidator));

        RuleFor(x => x.ExpiresAt)
            .IsInFuture(clock);
    }
}
