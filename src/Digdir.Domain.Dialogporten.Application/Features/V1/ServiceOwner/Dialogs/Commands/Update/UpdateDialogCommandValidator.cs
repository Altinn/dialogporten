﻿using Digdir.Domain.Dialogporten.Application.Common.Extensions.FluentValidation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Content;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

internal sealed class UpdateDialogCommandValidator : AbstractValidator<UpdateDialogCommand>
{
    public UpdateDialogCommandValidator(IValidator<UpdateDialogDto> updateDialogDtoValidator)
    {
        RuleFor(x => x.Id)
            .NotEmpty();
        RuleFor(x => x.Dto)
            .NotEmpty()
            .SetValidator(updateDialogDtoValidator);
    }
}

internal sealed class UpdateDialogDtoValidator : AbstractValidator<UpdateDialogDto>
{
    public UpdateDialogDtoValidator(
        IValidator<UpdateDialogDialogElementDto> elementValidator,
        IValidator<UpdateDialogDialogGuiActionDto> guiActionValidator,
        IValidator<UpdateDialogDialogApiActionDto> apiActionValidator,
        IValidator<UpdateDialogDialogActivityDto> activityValidator,
        IValidator<UpdateDialogSearchTagDto> searchTagValidator,
        IValidator<UpdateDialogContentDto> contentValidator)
    {
        // TODO: 'Progress' not allowed for correspondence dialogs
        // https://github.com/digdir/dialogporten/issues/233
        RuleFor(x => x.Progress)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.ExtendedStatus)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.ExternalReference)
            .MaximumLength(Constants.DefaultMaxStringLength);

        RuleFor(x => x.ExpiresAt)
            .GreaterThanOrEqualTo(x => x.DueAt)
                .WithMessage(FluentValidation_DateTimeOffset_Extensions.InFutureOfMessage)
                .When(x => x.DueAt.HasValue, ApplyConditionTo.CurrentValidator)
            .GreaterThanOrEqualTo(x => x.VisibleFrom)
                .WithMessage(FluentValidation_DateTimeOffset_Extensions.InFutureOfMessage)
                .When(x => x.VisibleFrom.HasValue, ApplyConditionTo.CurrentValidator);
        RuleFor(x => x.DueAt)
            .GreaterThanOrEqualTo(x => x.VisibleFrom)
                .WithMessage(FluentValidation_DateTimeOffset_Extensions.InFutureOfMessage)
                .When(x => x.VisibleFrom.HasValue, ApplyConditionTo.CurrentValidator);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.Content)
            .UniqueBy(x => x.Type)
            .Must(content => DialogContentType.RequiredTypes
                .All(requiredContent => content
                    .Select(x => x.Type)
                    .Contains(requiredContent)))
            .WithMessage($"Dialog must contain the following content: [{string.Join(", ", DialogContentType.RequiredTypes)}].")
            .ForEach(x => x.SetValidator(contentValidator));

        RuleFor(x => x.SearchTags)
            .UniqueBy(x => x.Value, StringComparer.InvariantCultureIgnoreCase)
            .ForEach(x => x.SetValidator(searchTagValidator));

        RuleFor(x => x.GuiActions)
            .Must(x => x
                .Count(x => x.Priority == DialogGuiActionPriority.Values.Primary) <= 1)
                .WithMessage("Only one primary GUI action is allowed.")
            .Must(x => x
                .Count(x => x.Priority == DialogGuiActionPriority.Values.Secondary) <= 1)
                .WithMessage("Only one secondary GUI action is allowed.")
            .Must(x => x
                .Count(x => x.Priority == DialogGuiActionPriority.Values.Tertiary) <= 5)
                .WithMessage("Only five tertiary GUI actions are allowed.")
            .UniqueBy(x => x.Id)
            .ForEach(x => x.SetValidator(guiActionValidator));

        RuleFor(x => x.ApiActions)
            .UniqueBy(x => x.Id);
        RuleForEach(x => x.ApiActions)
            .IsIn(x => x.Elements,
                dependentKeySelector: action => action.DialogElementId,
                principalKeySelector: element => element.Id)
            .SetValidator(apiActionValidator);

        RuleFor(x => x.Elements)
            .UniqueBy(x => x.Id);
        RuleForEach(x => x.Elements)
            .IsIn(x => x.Elements,
                dependentKeySelector: element => element.RelatedDialogElementId,
                principalKeySelector: element => element.Id)
            .SetValidator(elementValidator);

        RuleFor(x => x.Activities)
            .UniqueBy(x => x.Id);
        RuleForEach(x => x.Activities)
            .IsIn(x => x.Elements,
                dependentKeySelector: activity => activity.DialogElementId,
                principalKeySelector: element => element.Id)
            .IsIn(x => x.Activities,
                dependentKeySelector: activity => activity.RelatedActivityId,
                principalKeySelector: activity => activity.Id)
            .SetValidator(activityValidator);
    }
}

internal sealed class UpdateDialogContentDtoValidator : AbstractValidator<UpdateDialogContentDto>
{
    public UpdateDialogContentDtoValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum();
        RuleForEach(x => x.Value)
            .ContainsValidHtml()
            .When(x => DialogContentType.GetValue(x.Type).RenderAsHtml);
        RuleFor(x => x.Value)
            .NotEmpty()
            .SetValidator(x => new LocalizationDtosValidator(DialogContentType.GetValue(x.Type).MaxLength));
    }
}

internal sealed class UpdateDialogDialogElementDtoValidator : AbstractValidator<UpdateDialogDialogElementDto>
{
    public UpdateDialogDialogElementDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator,
        IValidator<UpdateDialogDialogElementUrlDto> urlValidator)
    {
        RuleFor(x => x.Id)
            .NotEqual(default(Guid));
        RuleFor(x => x.Type)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.ExternalReference)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.AuthorizationAttribute)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.RelatedDialogElementId)
            .NotEqual(x => x.Id)
            .When(x => x.RelatedDialogElementId.HasValue);
        RuleFor(x => x.DisplayName)
            .SetValidator(localizationsValidator);
        RuleFor(x => x.Urls)
            .UniqueBy(x => x.Id);
        RuleFor(x => x.Urls)
            .NotEmpty()
            .ForEach(x => x.SetValidator(urlValidator));
    }
}

internal sealed class UpdateDialogDialogElementUrlDtoValidator : AbstractValidator<UpdateDialogDialogElementUrlDto>
{
    public UpdateDialogDialogElementUrlDtoValidator()
    {
        RuleFor(x => x.Url)
            .NotNull()
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.MimeType)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.ConsumerType)
            .IsInEnum();
    }
}

internal sealed class UpdateDialogSearchTagDtoValidator : AbstractValidator<UpdateDialogSearchTagDto>
{
    public UpdateDialogSearchTagDtoValidator()
    {
        RuleFor(x => x.Value)
            .MinimumLength(3)
            .MaximumLength(Constants.MaxSearchTagLength);
    }
}

internal sealed class UpdateDialogDialogGuiActionDtoValidator : AbstractValidator<UpdateDialogDialogGuiActionDto>
{
    public UpdateDialogDialogGuiActionDtoValidator(
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
        RuleFor(x => x.Title)
            .NotEmpty()
            .SetValidator(localizationsValidator);
    }
}

internal sealed class UpdateDialogDialogApiActionDtoValidator : AbstractValidator<UpdateDialogDialogApiActionDto>
{
    public UpdateDialogDialogApiActionDtoValidator(
        IValidator<UpdateDialogDialogApiActionEndpointDto> apiActionEndpointValidator)
    {
        RuleFor(x => x.Action)
            .NotEmpty()
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.AuthorizationAttribute)
            .MaximumLength(Constants.DefaultMaxStringLength);
        RuleFor(x => x.Endpoints)
            .UniqueBy(x => x.Id);
        RuleFor(x => x.Endpoints)
            .NotEmpty()
            .ForEach(x => x.SetValidator(apiActionEndpointValidator));
    }
}

internal sealed class UpdateDialogDialogApiActionEndpointDtoValidator : AbstractValidator<UpdateDialogDialogApiActionEndpointDto>
{
    public UpdateDialogDialogApiActionEndpointDtoValidator()
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

internal sealed class UpdateDialogDialogActivityDtoValidator : AbstractValidator<UpdateDialogDialogActivityDto>
{
    public UpdateDialogDialogActivityDtoValidator(
        IValidator<IEnumerable<LocalizationDto>> localizationsValidator)
    {
        RuleFor(x => x.Id)
            .NotEqual(default(Guid));
        RuleFor(x => x.CreatedAt)
            .IsInPast();
        RuleFor(x => x.ExtendedType)
            .IsValidUri()
            .MaximumLength(Constants.DefaultMaxUriLength);
        RuleFor(x => x.Type)
            .IsInEnum();
        RuleFor(x => x.RelatedActivityId)
            .NotEqual(x => x.Id)
            .When(x => x.RelatedActivityId.HasValue);
        RuleFor(x => x.PerformedBy)
            .SetValidator(localizationsValidator);
        RuleFor(x => x.Description)
            .NotEmpty()
            .SetValidator(localizationsValidator);
    }
}
