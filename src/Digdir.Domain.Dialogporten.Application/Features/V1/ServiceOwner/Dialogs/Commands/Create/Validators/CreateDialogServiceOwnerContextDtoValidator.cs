using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogServiceOwnerContextDtoValidator : AbstractValidator<DialogServiceOwnerContextDto>
{
    public CreateDialogServiceOwnerContextDtoValidator(IValidator<ServiceOwnerLabelDto> serviceOwnerLabelValidator)
    {
        RuleFor(x => x.ServiceOwnerLabels)
            .UniqueBy(x => x.Value, StringComparer.InvariantCultureIgnoreCase)
            .Must(x => x.Count() <= DialogServiceOwnerLabel.MaxNumberOfLabels)
            .WithMessage($"Maximum {DialogServiceOwnerLabel.MaxNumberOfLabels} service owner labels are allowed.")
            .ForEach(x => x.SetValidator(serviceOwnerLabelValidator));
    }
}
