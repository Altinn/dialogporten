using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission.Validators;

internal sealed class CreateTransmissionTransmissionAttachmentDtoValidator : AbstractValidator<TransmissionAttachmentDto>
{
    public CreateTransmissionTransmissionAttachmentDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<TransmissionAttachmentUrlDto> urlValidator,
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
    }
}
