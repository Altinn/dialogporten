using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogCommandValidator : AbstractValidator<CreateDialogCommand>
{
    private const string IsApiOnlyKey = "IsApiOnly";

    public CreateDialogCommandValidator(IValidator<CreateDialogDto> createDialogDtoValidator)
    {
        RuleFor(x => x)
            .Custom((x, ctx) => ctx.RootContextData[IsApiOnlyKey] = x.Dto.IsApiOnly);

        RuleFor(x => x.Dto)
            .NotEmpty()
            .SetValidator(createDialogDtoValidator);
    }

    public static bool IsApiOnly<T>(T _, IValidationContext context)
        => context.RootContextData.TryGetValue(IsApiOnlyKey, out var isApiOnly) && (bool)isApiOnly;
}
