using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Common;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogDialogAttachmentDtoValidator : AbstractValidator<AttachmentDto>
{
    public UpdateDialogDialogAttachmentDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<AttachmentUrlDto> urlValidator,
        IValidator<AuthorizationContextDto> authorizationContextValidator,
        IClock clock)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast(clock);

        RuleFor(x => x.DisplayName)
            .SetValidator(localizationsValidator);

        RuleFor(x => x.Name)
            .MaximumLength(Constants.DefaultMaxStringLength)
            .When(x => x.Name is not null);

        RuleFor(x => x.Urls)
            .UniqueBy(x => x.Id);

        RuleFor(x => x.Urls)
            .NotEmpty()
            .ForEach(x => x.SetValidator(urlValidator));

        RuleFor(x => x.AuthorizationContext)
            .SetValidator(authorizationContextValidator!)
            .When(x => x.AuthorizationContext is not null);
    }
}
