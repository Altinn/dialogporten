using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.Http;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

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

internal sealed class UpdateDialogDtoValidator : AbstractValidator<UpdateDialogDto>
{
    public UpdateDialogDtoValidator(
        IValidator<TransmissionDto> transmissionValidator,
        IValidator<AttachmentDto> attachmentValidator,
        IValidator<GuiActionDto> guiActionValidator,
        IValidator<ApiActionDto> apiActionValidator,
        IValidator<ActivityDto> activityValidator,
        IValidator<SearchTagDto> searchTagValidator,
        IValidator<ServiceOwnerLabelDto> serviceOwnerLabelValidator,
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
            .When(x => x.DueAt.HasValue, ApplyConditionTo.CurrentValidator)
            .GreaterThanOrEqualTo(x => x.VisibleFrom)
            .WithMessage(FluentValidationDateTimeOffsetExtensions.InFutureOfMessage)
            .When(x => x.VisibleFrom.HasValue, ApplyConditionTo.CurrentValidator);
        RuleFor(x => x.DueAt)
            .GreaterThanOrEqualTo(x => x.VisibleFrom)
            .WithMessage(FluentValidationDateTimeOffsetExtensions.InFutureOfMessage)
            .When(x => x.VisibleFrom.HasValue, ApplyConditionTo.CurrentValidator);

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
                RuleFor(x => x.Content)
                    .NotEmpty()
                    .SetValidator(contentValidator));

        RuleForEach(x => x.Transmissions)
            .SetValidator(transmissionValidator);

        RuleFor(x => x.SearchTags)
            .UniqueBy(x => x.Value, StringComparer.InvariantCultureIgnoreCase)
            .ForEach(x => x.SetValidator(searchTagValidator));

        RuleFor(x => x.ServiceOwnerLabels)
            .UniqueBy(x => x.Value, StringComparer.InvariantCultureIgnoreCase)
            .ForEach(x => x.SetValidator(serviceOwnerLabelValidator));

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
}

internal sealed class UpdateDialogTransmissionAttachmentDtoValidator : AbstractValidator<TransmissionAttachmentDto>
{
    public UpdateDialogTransmissionAttachmentDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<TransmissionAttachmentUrlDto> urlValidator)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast();

        RuleFor(x => x.DisplayName)
            .SetValidator(localizationsValidator);
        RuleFor(x => x.Urls)
            .NotEmpty()
            .ForEach(x => x.SetValidator(urlValidator));
    }
}

internal sealed class UpdateDialogTransmissionAttachmentUrlDtoValidator : AbstractValidator<TransmissionAttachmentUrlDto>
{
    public UpdateDialogTransmissionAttachmentUrlDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.ConsumerType)
            .IsInEnum();
    }
}

internal sealed class UpdateDialogDialogTransmissionContentDtoValidator : AbstractValidator<TransmissionContentDto>
{
    private static readonly NullabilityInfoContext Context = new();
    private static readonly Dictionary<string, PropertyInfoWithNullability> SourcePropertyMetaDataByName = typeof(TransmissionContentDto)
        .GetProperties()
        .Select(x =>
        {
            var nullabilityInfo = Context.Create(x);
            return new PropertyInfoWithNullability(x, nullabilityInfo);
        })
        .ToDictionary(x => x.Property.Name, StringComparer.InvariantCultureIgnoreCase);

    public UpdateDialogDialogTransmissionContentDtoValidator()
    {
        foreach (var (propertyName, propMetadata) in SourcePropertyMetaDataByName)
        {
            switch (propMetadata.NullabilityInfo.WriteState)
            {
                case NullabilityState.NotNull:
                    RuleFor(x => propMetadata.Property.GetValue(x) as ContentValueDto)
                        .NotNull()
                        .WithMessage($"{propertyName} must not be empty.")
                        .SetValidator(
                            new ContentValueDtoValidator(DialogTransmissionContentType.Parse(propertyName))!);
                    break;
                case NullabilityState.Nullable:
                    RuleFor(x => propMetadata.Property.GetValue(x) as ContentValueDto)
                        .SetValidator(
                            new ContentValueDtoValidator(DialogTransmissionContentType.Parse(propertyName))!)
                        .When(x => propMetadata.Property.GetValue(x) is not null);
                    break;
                case NullabilityState.Unknown:
                    break;
                default:
                    break;
            }
        }
    }
}

internal sealed class UpdateDialogDialogTransmissionDtoValidator : AbstractValidator<TransmissionDto>
{
    public UpdateDialogDialogTransmissionDtoValidator(
        IValidator<ActorDto> actorValidator,
        IValidator<TransmissionContentDto?> contentValidator,
        IValidator<TransmissionAttachmentDto> attachmentValidator)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast();
        RuleFor(x => x.CreatedAt)
            .IsInPast();
        RuleFor(x => x.ExtendedType)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength)
            .When(x => x.ExtendedType is not null);
        RuleFor(x => x.Type)
            .IsInEnum();
        RuleFor(x => x.RelatedTransmissionId)
            .NotEqual(x => x.Id)
            .WithMessage(x => $"A transmission cannot reference itself ({nameof(x.RelatedTransmissionId)} is equal to {nameof(x.Id)}, '{x.Id}').")
            .When(x => x.RelatedTransmissionId.HasValue);
        RuleFor(x => x.Sender)
            .NotNull()
            .SetValidator(actorValidator);
        RuleFor(x => x.AuthorizationAttribute)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleForEach(x => x.Attachments)
            .SetValidator(attachmentValidator);

        When(UpdateDialogCommandValidator.IsApiOnly, () =>
                RuleFor(x => x.Content)
                    .SetValidator(contentValidator)
                    .When(x => x.Content is not null))
            .Otherwise(() =>
                RuleFor(x => x.Content)
                    .NotEmpty()
                    .SetValidator(contentValidator));
    }
}

internal sealed class UpdateDialogContentDtoValidator : AbstractValidator<ContentDto>
{
    private static readonly NullabilityInfoContext Context = new();
    private static readonly Dictionary<string, PropertyInfoWithNullability> SourcePropertyMetaDataByName =
        typeof(ContentDto).GetProperties()
            .Select(x =>
            {
                var nullabilityInfo = Context.Create(x);
                return new PropertyInfoWithNullability(x, nullabilityInfo);
            })
            .ToDictionary(x => x.Property.Name, StringComparer.InvariantCultureIgnoreCase);

    public UpdateDialogContentDtoValidator(IUser? user)
    {
        foreach (var (propertyName, propMetadata) in SourcePropertyMetaDataByName)
        {
            switch (propMetadata.NullabilityInfo.WriteState)
            {
                case NullabilityState.NotNull:
                    RuleFor(x => propMetadata.Property.GetValue(x) as ContentValueDto)
                        .NotNull()
                        .WithMessage($"{propertyName} must not be empty.")
                        .SetValidator(
                            new ContentValueDtoValidator(DialogContentType.Parse(propertyName), user)!);
                    break;
                case NullabilityState.Nullable:
                    RuleFor(x => propMetadata.Property.GetValue(x) as ContentValueDto)
                        .SetValidator(
                            new ContentValueDtoValidator(DialogContentType.Parse(propertyName), user)!)
                        .When(x => propMetadata.Property.GetValue(x) is not null);
                    break;
                case NullabilityState.Unknown:
                    break;
                default:
                    break;
            }
        }
    }
}

internal sealed class UpdateDialogDialogAttachmentDtoValidator : AbstractValidator<AttachmentDto>
{
    public UpdateDialogDialogAttachmentDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<AttachmentUrlDto> urlValidator)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast();
        RuleFor(x => x.DisplayName)
            .SetValidator(localizationsValidator);
        RuleFor(x => x.Urls)
            .UniqueBy(x => x.Id);
        RuleFor(x => x.Urls)
            .NotEmpty()
            .ForEach(x => x.SetValidator(urlValidator));
    }
}

internal sealed class UpdateDialogDialogAttachmentUrlDtoValidator : AbstractValidator<AttachmentUrlDto>
{
    public UpdateDialogDialogAttachmentUrlDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.ConsumerType)
            .IsInEnum();
    }
}

internal sealed class UpdateDialogSearchTagDtoValidator : AbstractValidator<SearchTagDto>
{
    public UpdateDialogSearchTagDtoValidator()
    {
        RuleFor(x => x.Value)
            .MinimumLength(3)
            .MaximumLength(Constants.MaxSearchTagLength);
    }
}

internal sealed class UpdateDialogServiceOwnerLabelDtoValidator : AbstractValidator<ServiceOwnerLabelDto>
{
    public UpdateDialogServiceOwnerLabelDtoValidator()
    {
        RuleFor(x => x.Value)
            .MinimumLength(3)
            .MaximumLength(Constants.MaxSearchTagLength);
    }
}

internal sealed class UpdateDialogDialogGuiActionDtoValidator : AbstractValidator<GuiActionDto>
{
    public UpdateDialogDialogGuiActionDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator)
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.AuthorizationAttribute)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.Priority)
            .IsInEnum();
        RuleFor(x => x.HttpMethod)
            .Must(x => x is HttpVerb.Values.GET or HttpVerb.Values.POST or HttpVerb.Values.DELETE)
            .WithMessage($"'{{PropertyName}}' for GUI actions must be one of the following: " +
                         $"[{HttpVerb.Values.GET}, {HttpVerb.Values.POST}, {HttpVerb.Values.DELETE}].");
        RuleFor(x => x.Title)
            .NotEmpty()
            .SetValidator(localizationsValidator);
        RuleFor(x => x.Prompt)
            .SetValidator(localizationsValidator!)
            .When(x => x.Prompt != null);
    }
}

internal sealed class UpdateDialogDialogApiActionDtoValidator : AbstractValidator<ApiActionDto>
{
    public UpdateDialogDialogApiActionDtoValidator(
        IValidator<ApiActionEndpointDto> apiActionEndpointValidator)
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.AuthorizationAttribute)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.Name)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.Endpoints)
            .UniqueBy(x => x.Id);
        RuleFor(x => x.Endpoints)
            .NotEmpty()
            .ForEach(x => x.SetValidator(apiActionEndpointValidator));
    }
}

internal sealed class UpdateDialogDialogApiActionEndpointDtoValidator : AbstractValidator<ApiActionEndpointDto>
{
    public UpdateDialogDialogApiActionEndpointDtoValidator()
    {
        RuleFor(x => x.Version)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidHttpsUrl()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.HttpMethod)
            .IsInEnum();
        RuleFor(x => x.DocumentationUrl)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.RequestSchema)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.ResponseSchema)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.Deprecated)
            .Equal(true)
            .WithMessage($"'{{PropertyName}}' must be equal to 'True' when {nameof(ApiActionEndpointDto.SunsetAt)} is set.")
            .When(x => x.SunsetAt.HasValue);
    }
}

internal sealed class UpdateDialogDialogActivityDtoValidator : AbstractValidator<ActivityDto>
{
    public UpdateDialogDialogActivityDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<ActorDto> actorValidator)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast();
        RuleFor(x => x.CreatedAt)
            .IsInPast();
        RuleFor(x => x.ExtendedType)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.Type)
            .IsInEnum();
        RuleFor(x => x.PerformedBy)
            .NotNull()
            .SetValidator(actorValidator);
        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required when the type is '" + nameof(DialogActivityType.Values.Information) + "'.")
            .SetValidator(localizationsValidator)
            .When(x => x.Type == DialogActivityType.Values.Information);
        RuleFor(x => x.Description)
            .Empty()
            .WithMessage("Description is only allowed when the type is '" + nameof(DialogActivityType.Values.Information) + "'.")
            .When(x => x.Type != DialogActivityType.Values.Information);
        RuleFor(x => x.TransmissionId)
            .Null()
            .WithMessage($"Only activities of type {nameof(DialogActivityType.Values.TransmissionOpened)} can reference a transmission.")
            .When(x => x.Type != DialogActivityType.Values.TransmissionOpened);
        RuleFor(x => x.TransmissionId)
            .NotEmpty()
            .WithMessage($"An activity of type {nameof(DialogActivityType.Values.TransmissionOpened)} needs to reference a transmission.")
            .When(x => x.Type == DialogActivityType.Values.TransmissionOpened);
    }
}
