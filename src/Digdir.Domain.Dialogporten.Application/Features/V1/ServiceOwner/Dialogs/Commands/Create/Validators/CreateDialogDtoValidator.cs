using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.Validators;

internal sealed class CreateDialogDtoValidator : AbstractValidator<CreateDialogDto>
{
    public CreateDialogDtoValidator(IValidator<TransmissionDto> transmissionValidator,
        IValidator<AttachmentDto> attachmentValidator,
        IValidator<GuiActionDto> guiActionValidator,
        IValidator<ApiActionDto> apiActionValidator,
        IValidator<ActivityDto> activityValidator,
        IValidator<SearchTagDto> searchTagValidator,
        IValidator<ContentDto?> contentValidator,
        IValidator<DialogServiceOwnerContextDto?> serviceOwnerContextValidator)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast();

        RuleFor(x => x.CreatedAt)
            .IsInPast();

        RuleFor(x => x.CreatedAt)
            .NotEmpty()
            .WithMessage($"{{PropertyName}} must not be empty when '{nameof(CreateDialogDto.UpdatedAt)} is set.")
            .When(x => x.UpdatedAt.HasValue && x.UpdatedAt != default(DateTimeOffset));

        RuleFor(x => x.UpdatedAt)
            .IsInPast()
            .GreaterThanOrEqualTo(x => x.CreatedAt)
            .WithMessage($"'{{PropertyName}}' must be greater than or equal to '{nameof(CreateDialogDto.CreatedAt)}'.")
            .When(x => x.CreatedAt.HasValue && x.CreatedAt != default(DateTimeOffset) &&
                       x.UpdatedAt.HasValue && x.UpdatedAt != default(DateTimeOffset));

        RuleFor(x => x.IdempotentKey)
            .MaximumLength(36);

        RuleFor(x => x.ServiceResource)
            .NotNull()
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength)
            .Must(x =>
                x?.StartsWith(Constants.ServiceResourcePrefix, StringComparison.InvariantCulture) ?? false)
            .WithMessage($"'{{PropertyName}}' must start with '{Constants.ServiceResourcePrefix}'.");

        RuleFor(x => x.Party)
            .IsValidPartyIdentifier()
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.Progress)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.ExtendedStatus)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.ExternalReference)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.ExpiresAt)
            .IsInFuture()
            .GreaterThanOrEqualTo(x => x.DueAt)
            .WithMessage(FluentValidationDateTimeOffsetExtensions.InFutureOfMessage)
            .When(x => x.DueAt.HasValue, ApplyConditionTo.CurrentValidator)
            .GreaterThanOrEqualTo(x => x.VisibleFrom)
            .WithMessage(FluentValidationDateTimeOffsetExtensions.InFutureOfMessage)
            .When(x => x.VisibleFrom.HasValue, ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.DueAt)
            .IsInFuture()
            .GreaterThanOrEqualTo(x => x.VisibleFrom)
            .WithMessage(FluentValidationDateTimeOffsetExtensions.InFutureOfMessage)
            .When(x => x.VisibleFrom.HasValue, ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.VisibleFrom)
            .IsInFuture();

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);

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

        RuleFor(x => x.Transmissions)
            .UniqueBy(x => x.Id);

        RuleForEach(x => x.Transmissions)
            .IsIn(x => x.Transmissions,
                dependentKeySelector: transmission => transmission.RelatedTransmissionId,
                principalKeySelector: transmission => transmission.Id);

        // When IsApiOnly is set to true, we only validate content if it's provided
        // on both the dialog and the transmission level.
        When(CreateDialogCommandValidator.IsApiOnly, () =>
                RuleFor(x => x.Content)
                    .SetValidator(contentValidator)
                    .When(x => x.Content is not null))
            .Otherwise(() =>
                RuleFor(x => x.Content)
                    .NotEmpty()
                    .SetValidator(contentValidator));

        RuleForEach(x => x.Transmissions)
            .SetValidator(transmissionValidator);

        RuleFor(x => x.Activities)
            .UniqueBy(x => x.Id);

        RuleForEach(x => x.Activities)
            .IsIn(x => x.Transmissions,
                dependentKeySelector: activity => activity.TransmissionId,
                principalKeySelector: transmission => transmission.Id)
            .SetValidator(activityValidator);

        RuleFor(x => x.Process)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength)
            .When(x => x.Process is not null);

        RuleFor(x => x.Process)
            .NotEmpty()
            .WithMessage($"{{PropertyName}} must not be empty when {nameof(CreateDialogDto.PrecedingProcess)} is set.")
            .When(x => x.PrecedingProcess is not null);

        RuleFor(x => x.PrecedingProcess)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength)
            .When(x => x.PrecedingProcess is not null);

        RuleFor(x => x.ServiceOwnerContext)
            .SetValidator(serviceOwnerContextValidator)
            .When(x => x.ServiceOwnerContext is not null);

        RuleFor(x => x.SystemLabel)
            .Must(x => SystemLabel.IsDefaultArchiveBinGroup(x.GetValueOrDefault()))
            .When(x => x.SystemLabel is not null)
            .WithMessage($"{{PropertyName}} must be {SystemLabel.Values.Default}, {SystemLabel.Values.Bin} or {SystemLabel.Values.Archive}.");
    }
}
