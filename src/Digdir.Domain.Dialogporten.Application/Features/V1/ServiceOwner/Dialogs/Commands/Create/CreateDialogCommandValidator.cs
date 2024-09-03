﻿using System.Reflection;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.Enumerables;
using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.Http;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

internal sealed class CreateDialogCommandValidator : AbstractValidator<CreateDialogCommand>
{
    public CreateDialogCommandValidator(
        IValidator<CreateDialogDialogTransmissionDto> transmissionValidator,
        IValidator<CreateDialogDialogAttachmentDto> attachmentValidator,
        IValidator<CreateDialogDialogGuiActionDto> guiActionValidator,
        IValidator<CreateDialogDialogApiActionDto> apiActionValidator,
        IValidator<CreateDialogDialogActivityDto> activityValidator,
        IValidator<CreateDialogSearchTagDto> searchTagValidator,
        IValidator<CreateDialogContentDto> contentValidator)
    {
        RuleFor(x => x.Id)
            .IsValidUuidV7()
            .UuidV7TimestampIsInPast();

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
            .IsInEnum();

        RuleFor(x => x.Content)
            .SetValidator(contentValidator);

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
            .ForEach(x => x.SetValidator(guiActionValidator));

        RuleForEach(x => x.ApiActions)
            .SetValidator(apiActionValidator);

        RuleForEach(x => x.Attachments)
            .SetValidator(attachmentValidator);

        RuleFor(x => x.Transmissions)
            .UniqueBy(x => x.Id);
        RuleForEach(x => x.Transmissions)
            .IsIn(x => x.Transmissions,
                dependentKeySelector: transmission => transmission.RelatedTransmissionId,
                principalKeySelector: transmission => transmission.Id)
            .SetValidator(transmissionValidator);

        RuleFor(x => x.Activities)
            .UniqueBy(x => x.Id);
        RuleForEach(x => x.Activities)
            .IsIn(x => x.Transmissions,
                dependentKeySelector: activity => activity.TransmissionId,
                principalKeySelector: transmission => transmission.Id)
            .IsIn(x => x.Activities,
                dependentKeySelector: activity => activity.RelatedActivityId,
                principalKeySelector: activity => activity.Id)
            .SetValidator(activityValidator);
    }
}

internal sealed class CreateDialogDialogTransmissionDtoValidator : AbstractValidator<CreateDialogDialogTransmissionDto>
{
    public CreateDialogDialogTransmissionDtoValidator(
        IValidator<CreateDialogDialogTransmissionSenderActorDto> actorValidator,
        IValidator<CreateDialogDialogTransmissionContentDto> contentValidator,
        IValidator<CreateDialogTransmissionAttachmentDto> attachmentValidator)
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
        RuleFor(x => x.Content)
            .NotEmpty()
            .SetValidator(contentValidator);
    }
}

internal sealed class CreateDialogContentDtoValidator : AbstractValidator<CreateDialogContentDto?>
{
    private static readonly Dictionary<string, PropertyInfoWithNullability> SourcePropertyMetaDataByName = typeof(CreateDialogContentDto)
        .GetProperties()
        .Select(x =>
        {
            var context = new NullabilityInfoContext();
            var nullabilityInfo = context.Create(x);

            return new PropertyInfoWithNullability(x, nullabilityInfo);
        })
        .ToDictionary(x => x.Property.Name, StringComparer.InvariantCultureIgnoreCase);

    public CreateDialogContentDtoValidator(IUser? user)
    {
        foreach (var (propertyName, propMetadata) in SourcePropertyMetaDataByName)
        {
            switch (propMetadata.NullabilityInfo.WriteState)
            {
                case NullabilityState.NotNull:
                    RuleFor(x => propMetadata.Property.GetValue(x) as ContentValueDto)
                        .NotNull()
                        .WithMessage($"{propertyName} must not be empty.")
                        .SetValidator(new ContentValueDtoValidator(
                            DialogContentType.Parse(propertyName), user)!);
                    break;
                case NullabilityState.Nullable:
                    RuleFor(x => propMetadata.Property.GetValue(x) as ContentValueDto)
                        .SetValidator(new ContentValueDtoValidator(
                            DialogContentType.Parse(propertyName), user)!)
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

internal sealed class CreateDialogDialogTransmissionContentDtoValidator : AbstractValidator<CreateDialogDialogTransmissionContentDto>
{
    private static readonly Dictionary<string, PropertyInfo> SourcePropertyMetaDataByName = typeof(CreateDialogDialogTransmissionContentDto)
        .GetProperties()
        .ToDictionary(x => x.Name, StringComparer.InvariantCultureIgnoreCase);

    public CreateDialogDialogTransmissionContentDtoValidator()
    {
        foreach (var (propertyName, propMetadata) in SourcePropertyMetaDataByName)
        {
            RuleFor(x => propMetadata.GetValue(x) as ContentValueDto)
                .NotNull()
                .WithMessage($"{propertyName} must not be empty.")
                .SetValidator(new ContentValueDtoValidator(DialogTransmissionContentType.Parse(propertyName))!);
        }
    }
}

internal sealed class CreateDialogDialogAttachmentDtoValidator : AbstractValidator<CreateDialogDialogAttachmentDto>
{
    public CreateDialogDialogAttachmentDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<CreateDialogDialogAttachmentUrlDto> urlValidator)
    {
        RuleFor(x => x.DisplayName)
            .SetValidator(localizationsValidator);
        RuleFor(x => x.Urls)
            .NotEmpty()
            .ForEach(x => x.SetValidator(urlValidator));
    }
}

internal sealed class CreateDialogDialogAttachmentUrlDtoValidator : AbstractValidator<CreateDialogDialogAttachmentUrlDto>
{
    public CreateDialogDialogAttachmentUrlDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.ConsumerType)
            .IsInEnum();
    }
}

internal sealed class CreateDialogTransmissionAttachmentDtoValidator : AbstractValidator<CreateDialogTransmissionAttachmentDto>
{
    public CreateDialogTransmissionAttachmentDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<CreateDialogTransmissionAttachmentUrlDto> urlValidator)
    {
        RuleFor(x => x.DisplayName)
            .SetValidator(localizationsValidator);
        RuleFor(x => x.Urls)
            .NotEmpty()
            .ForEach(x => x.SetValidator(urlValidator));
    }
}

internal sealed class CreateDialogTransmissionAttachmentUrlDtoValidator : AbstractValidator<CreateDialogTransmissionAttachmentUrlDto>
{
    public CreateDialogTransmissionAttachmentUrlDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.ConsumerType)
            .IsInEnum();
    }
}

internal sealed class CreateDialogSearchTagDtoValidator : AbstractValidator<CreateDialogSearchTagDto>
{
    public CreateDialogSearchTagDtoValidator()
    {
        RuleFor(x => x.Value)
            .MinimumLength(3)
            .MaximumLength(Constants.MaxSearchTagLength);
    }
}

internal sealed class CreateDialogDialogGuiActionDtoValidator : AbstractValidator<CreateDialogDialogGuiActionDto>
{
    public CreateDialogDialogGuiActionDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator)
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidUri()
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

internal sealed class CreateDialogDialogApiActionDtoValidator : AbstractValidator<CreateDialogDialogApiActionDto>
{
    public CreateDialogDialogApiActionDtoValidator(
        IValidator<CreateDialogDialogApiActionEndpointDto> apiActionEndpointValidator)
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.AuthorizationAttribute)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.Endpoints)
            .NotEmpty()
            .ForEach(x => x.SetValidator(apiActionEndpointValidator));
    }
}

internal sealed class CreateDialogDialogApiActionEndpointDtoValidator : AbstractValidator<CreateDialogDialogApiActionEndpointDto>
{
    public CreateDialogDialogApiActionEndpointDtoValidator()
    {
        RuleFor(x => x.Version)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidUri()
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
            .When(x => x.SunsetAt.HasValue);
    }
}

internal sealed class CreateDialogDialogActivityDtoValidator : AbstractValidator<CreateDialogDialogActivityDto>
{
    public CreateDialogDialogActivityDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<CreateDialogDialogActivityPerformedByActorDto> actorValidator)
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
        RuleFor(x => x.RelatedActivityId)
            .NotEqual(x => x.Id)
            .WithMessage(x => $"An activity cannot reference itself ({nameof(x.RelatedActivityId)} is equal to {nameof(x.Id)}, '{x.Id}').")
            .When(x => x.RelatedActivityId.HasValue);
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
    }
}

internal sealed class CreateDialogDialogTransmissionActorDtoValidator : AbstractValidator<CreateDialogDialogTransmissionSenderActorDto>
{
    public CreateDialogDialogTransmissionActorDtoValidator()
    {
        RuleFor(x => x.ActorType)
            .IsInEnum();

        RuleFor(x => x.ActorId)
            .Must((dto, value) => value is null || dto.ActorName is null)
            .WithMessage("Only one of 'ActorId' or 'ActorName' can be set, but not both.");

        RuleFor(x => x.ActorType)
            .Must((dto, value) => (value == ActorType.Values.ServiceOwner && dto.ActorId is null && dto.ActorName is null) ||
                                  (value != ActorType.Values.ServiceOwner && (dto.ActorId is not null || dto.ActorName is not null)))
            .WithMessage("If 'ActorType' is 'ServiceOwner', both 'ActorId' and 'ActorName' must be null. Otherwise, one of them must be set.");

        RuleFor(x => x.ActorId!)
            .IsValidPartyIdentifier()
            .When(x => x.ActorId is not null);
    }
}

internal sealed class CreateDialogDialogActivityActorDtoValidator : AbstractValidator<CreateDialogDialogActivityPerformedByActorDto>
{
    public CreateDialogDialogActivityActorDtoValidator()
    {
        RuleFor(x => x.ActorType)
            .IsInEnum();

        RuleFor(x => x.ActorId)
            .Must((dto, value) => value is null || dto.ActorName is null)
            .WithMessage("Only one of 'ActorId' or 'ActorName' can be set, but not both.");

        RuleFor(x => x.ActorType)
            .Must((dto, value) => (value == ActorType.Values.ServiceOwner && dto.ActorId is null && dto.ActorName is null) ||
                                  (value != ActorType.Values.ServiceOwner && (dto.ActorId is not null || dto.ActorName is not null)))
            .WithMessage("If 'ActorType' is 'ServiceOwner', both 'ActorId' and 'ActorName' must be null. Otherwise, one of them must be set.");

        RuleFor(x => x.ActorId!)
            .IsValidPartyIdentifier()
            .When(x => x.ActorId is not null);
    }
}
