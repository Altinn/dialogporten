using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;

internal sealed class UpdateDialogServiceOwnerContextCommandValidator : AbstractValidator<UpdateDialogServiceOwnerContextCommand>
{
    public UpdateDialogServiceOwnerContextCommandValidator(
        IValidator<UpdateServiceOwnerContextDto> updateServiceOwnerContextDtoValidator)
    {
        RuleFor(x => x.DialogId)
            .NotEmpty();

        RuleFor(x => x.Dto)
            .NotEmpty()
            .SetValidator(updateServiceOwnerContextDtoValidator);
    }
}

public sealed class UpdateServiceOwnerContextDtoValidator : AbstractValidator<UpdateServiceOwnerContextDto>
{
    public UpdateServiceOwnerContextDtoValidator(IValidator<ServiceOwnerLabelDto> serviceOwnerLabelValidator)
    {
        RuleFor(x => x.ServiceOwnerLabels)
            .UniqueBy(x => x.Value, StringComparer.InvariantCultureIgnoreCase)
            .Must(x => x.Count() <= DialogServiceOwnerLabel.MaxNumberOfLabels)
            .WithMessage($"Maximum {DialogServiceOwnerLabel.MaxNumberOfLabels} service owner labels are allowed.")
            .ForEach(x => x.SetValidator(serviceOwnerLabelValidator));
    }
}

internal sealed class CreateDialogServiceOwnerLabelDtoValidator : AbstractValidator<ServiceOwnerLabelDto>
{
    public CreateDialogServiceOwnerLabelDtoValidator()
    {
        RuleFor(x => x.Value)
            .MinimumLength(Constants.MinSearchStringLength)
            .MaximumLength(Constants.DefaultMaxStringLength);
    }
}
