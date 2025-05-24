using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogDialogActivityDtoValidator : AbstractValidator<ActivityDto>
{
    public CreateDialogDialogActivityDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<ActorDto> actorValidator)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast();
        RuleFor(x => x.CreatedAt)
            .IsInPast();
        RuleFor(x => x.ExtendedType)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.Type)
            .IsInEnum();
        RuleFor(x => x.PerformedBy)
            .NotNull()
            .SetValidator(actorValidator);
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required when the type is '" + nameof(DialogActivityType.Values.Information) + "'.")
            .SetValidator(localizationsValidator)
            .When(x => x.Type == DialogActivityType.Values.Information);
        RuleFor(x => x.Description)
            .Empty()
            .WithMessage("Description is only allowed when the type is '" + nameof(DialogActivityType.Values.Information) + "'.")
            .When(x => x.Type != DialogActivityType.Values.Information);
        RuleFor(x => x.TransmissionId)
            .Null()
            .WithMessage($"Only activities of type {nameof(DialogActivityType.Values.TransmissionOpened)} can reference a transmission.")
            .When(x => x.Type != DialogActivityType.Values.TransmissionOpened);
        RuleFor(x => x.TransmissionId)
            .NotEmpty()
            .WithMessage($"An activity of type {nameof(DialogActivityType.Values.TransmissionOpened)} needs to reference a transmission.")
            .When(x => x.Type == DialogActivityType.Values.TransmissionOpened);
    }
}