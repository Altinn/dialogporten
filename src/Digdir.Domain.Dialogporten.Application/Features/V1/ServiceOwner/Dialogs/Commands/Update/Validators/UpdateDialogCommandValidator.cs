using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogCommandValidator : AbstractValidator<UpdateDialogCommand>
{
    public UpdateDialogCommandValidator(
        IValidator<UpdateDialogDto> updateDialogDtoValidator)
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Dto)
            .NotEmpty()
            .SetValidator(updateDialogDtoValidator)
            .When(DialogIsPreloaded);
    }

    public static bool IsApiOnly<T>(T _, IValidationContext context)
        => UpdateDialogDataLoader.GetPreloadedData(context)?.IsApiOnly ?? false;

    private static bool DialogIsPreloaded<T>(T _, IValidationContext context)
        => context.RootContextData.TryGetValue(UpdateDialogDataLoader.Key, out var dialog) &&
           dialog is not null;
}
