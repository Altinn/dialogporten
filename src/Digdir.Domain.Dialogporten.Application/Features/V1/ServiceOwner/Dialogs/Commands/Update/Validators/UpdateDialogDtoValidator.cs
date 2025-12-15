using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.Validators;

internal sealed class UpdateDialogDtoValidator : AbstractValidator<UpdateDialogDto>
{
    public UpdateDialogDtoValidator(
        IValidator<TransmissionDto> transmissionValidator,
        IValidator<AttachmentDto> attachmentValidator,
        IValidator<GuiActionDto> guiActionValidator,
        IValidator<ApiActionDto> apiActionValidator,
        IValidator<ActivityDto> activityValidator,
        IValidator<SearchTagDto> searchTagValidator,
        IValidator<ContentDto?> contentValidator)
    {
        RuleFor(x => x.Progress)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.ExtendedStatus)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.ExternalReference)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.ExpiresAt)
            .GreaterThanOrEqualTo(x => x.DueAt)
            .WithMessage(FluentValidationDateTimeOffsetExtensions.InFutureOfMessage)
            .When(x => x.DueAt.HasValue, ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Transmissions)
            .UniqueBy(x => x.Id);

        // When IsApiOnly is set to true, we only validate content if it's provided
        // on both the dialog and the transmission level.
        When(UpdateDialogCommandValidator.IsApiOnly, () =>
                RuleFor(x => x.Content)
                    .SetValidator(contentValidator)
                    .When(x => x.Content is not null))
            .Otherwise(() =>
            {
                RuleFor(x => x.Content)
                    .NotEmpty()
                    .SetValidator(contentValidator);

                RuleFor(x => x.Transmissions)
                    .Must((_, _, ctx) => UpdateDialogDataLoader
                        .GetPreloadedData(ctx)!.Transmissions
                        .All(x => x.Content.Count != 0))
                    .WithMessage(
                        $"This dialog is locked to {nameof(DialogDto.IsApiOnly)}=true, as one or " +
                        $"more of the existing immutable transmissions do not have content.");
            });

        const string visibleFromErrorMessage = "{PropertyName} must be greater than or equal to VisibleFrom.";
        RuleFor(x => x.DueAt)
            .Must((_, dueAt, context) =>
            {
                var visibleFrom = GetVisibleFrom(context);
                return visibleFrom is null || dueAt is null || dueAt >= visibleFrom;
            })
            .WithMessage(visibleFromErrorMessage)
            .When(DialogHasVisibleFrom);

        RuleFor(x => x.ExpiresAt)
            .Must((_, expiresAt, context) =>
            {
                var visibleFrom = GetVisibleFrom(context);
                return visibleFrom is null || expiresAt is null || expiresAt >= visibleFrom;
            })
            .WithMessage(visibleFromErrorMessage)
            .When(DialogHasVisibleFrom);

        RuleForEach(x => x.Transmissions)
            .SetValidator(transmissionValidator);

        RuleFor(x => x.SearchTags)
            .UniqueBy(x => x.Value, StringComparer.InvariantCultureIgnoreCase)
            .ForEach(x => x.SetValidator(searchTagValidator));

        RuleFor(x => x.GuiActions)
            .Must(x => x
                .EmptyIfNull()
                .Count(x => x.Priority == DialogGuiActionPriority.Values.Primary) <= 1)
            .WithMessage("Only one primary GUI action is allowed.")
            .Must(x => x
                .EmptyIfNull()
                .Count(x => x.Priority == DialogGuiActionPriority.Values.Secondary) <= 1)
            .WithMessage("Only one secondary GUI action is allowed.")
            .Must(x => x
                .EmptyIfNull()
                .Count(x => x.Priority == DialogGuiActionPriority.Values.Tertiary) <= 5)
            .WithMessage("Only five tertiary GUI actions are allowed.")
            .UniqueBy(x => x.Id)
            .ForEach(x => x.SetValidator(guiActionValidator));

        RuleFor(x => x.ApiActions)
            .UniqueBy(x => x.Id);

        RuleForEach(x => x.ApiActions)
            .SetValidator(apiActionValidator);

        RuleFor(x => x.Attachments)
            .UniqueBy(x => x.Id);

        RuleForEach(x => x.Attachments)
            .SetValidator(attachmentValidator);

        RuleFor(x => x.Activities)
            .UniqueBy(x => x.Id);

        RuleForEach(x => x.Activities)
            .SetValidator(activityValidator);

        RuleFor(x => x.Process)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength)
            .When(x => x.Process is not null);

        RuleFor(x => x.Process)
            .NotEmpty()
            .WithMessage($"{{PropertyName}} must not be empty when {nameof(UpdateDialogDto.PrecedingProcess)} is set.")
            .When(x => x.PrecedingProcess is not null);

        RuleFor(x => x.PrecedingProcess)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength)
            .When(x => x.PrecedingProcess is not null);
    }
    private static bool DialogHasVisibleFrom<T>(T _, IValidationContext context) =>
        GetVisibleFrom(context).HasValue;

    private static DateTimeOffset? GetVisibleFrom(IValidationContext context) =>
        UpdateDialogDataLoader.GetPreloadedData(context)?.VisibleFrom;
}
