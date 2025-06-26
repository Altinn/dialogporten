using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateFormSavedActivityTime;

internal sealed class UpdateFormSavedActivityTimeCommandValidator : AbstractValidator<UpdateFormSavedActivityTimeCommand>
{
    public UpdateFormSavedActivityTimeCommandValidator()
    {
        RuleFor(x => x.DialogId).NotEmpty();
        RuleFor(x => x.ActivityId).NotEmpty();
        RuleFor(x => x.NewCreatedAt).IsInPast();
    }
}
