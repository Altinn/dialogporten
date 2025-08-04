using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;

public sealed class SetSystemLabelCommandValidator : AbstractValidator<SetSystemLabelCommand>
{
    public SetSystemLabelCommandValidator()
    {
    }
}
