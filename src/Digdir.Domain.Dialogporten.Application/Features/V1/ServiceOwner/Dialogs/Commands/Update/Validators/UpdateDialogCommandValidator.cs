using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogCommandValidator : AbstractValidator<UpdateDialogCommand>
{
    private const string IsApiOnlyKey = "IsApiOnly";

    public UpdateDialogCommandValidator(
        IValidator<UpdateDialogDto> updateDialogDtoValidator)
    {
        RuleFor(x => x)
            .Custom((x, ctx) => ctx.RootContextData[IsApiOnlyKey] = x.Dto.IsApiOnly);

        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Dto)
            .NotEmpty()
            .SetValidator(updateDialogDtoValidator)
            .WhenAsync(DialogExistsAsync);
    }

    public static bool IsApiOnly<T>(T _, IValidationContext context)
        => context.RootContextData.TryGetValue(IsApiOnlyKey, out var isApiOnly) && (bool)isApiOnly;

    private static async Task<bool> DialogExistsAsync(
        UpdateDialogCommand _,
        ValidationContext<UpdateDialogCommand> context,
        CancellationToken cancellationToken) => await UpdateDialogExistsDataLoader.GetPreloadedDataAsync(context);
}
