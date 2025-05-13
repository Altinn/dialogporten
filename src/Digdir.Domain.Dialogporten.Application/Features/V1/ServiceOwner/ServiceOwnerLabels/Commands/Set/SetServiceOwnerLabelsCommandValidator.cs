using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.ServiceOwnerContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerLabels.Commands.Set;

public sealed class SetServiceOwnerLabelsCommandValidator : AbstractValidator<SetServiceOwnerLabelsCommand>
{
    public SetServiceOwnerLabelsCommandValidator(
        IValidator<ServiceOwnerLabelDto> serviceOwnerLabelValidator)
    {
        RuleFor(x => x.DialogId)
            .NotEmpty();

        RuleFor(x => x.ServiceOwnerLabels)
            .UniqueBy(x => x.Value, StringComparer.InvariantCultureIgnoreCase)
            .Must(x => x.Count() <= ServiceOwnerLabel.MaxNumberOfLabels)
            .WithMessage($"Maximum {ServiceOwnerLabel.MaxNumberOfLabels} service owner labels are allowed.")
            .ForEach(x => x.SetValidator(serviceOwnerLabelValidator));
    }
}

internal sealed class ServiceOwnerLabelDtoValidator : AbstractValidator<ServiceOwnerLabelDto>
{
    public ServiceOwnerLabelDtoValidator()
    {
        RuleFor(x => x.Value)
            .MinimumLength(Constants.MinSearchStringLength)
            .MaximumLength(Constants.DefaultMaxStringLength);
    }
}
