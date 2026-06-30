using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Http;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

public static class Mappers
{
    public static UpdateDialogDto ToUpdateDialogDto(this DialogDto source) =>
        new()
        {
            Progress = source.Progress,
            ExtendedStatus = source.ExtendedStatus,
            ExternalReference = source.ExternalReference,
            DueAt = source.DueAt,
            Process = source.Process,
            PrecedingProcess = source.PrecedingProcess,
            ExpiresAt = source.ExpiresAt,
            IsApiOnly = source.IsApiOnly,
            Status = source.Status.ToDialogStatusInput(),
            Content = source.Content.ToUpdateContentDto(),
            SearchTags = source.SearchTags?.Select(x => new SearchTagDto { Value = x.Value }).ToList() ?? [],
            Attachments = source.Attachments.Select(x => x.ToUpdateAttachmentDto()).ToList(),
            GuiActions = source.GuiActions.Select(x => x.ToUpdateGuiActionDto()).ToList(),
            ApiActions = source.ApiActions.Select(x => x.ToUpdateApiActionDto()).ToList(),
            // Activities and transmissions are append-only, and PATCH must not replay existing records.
            Activities = [],
            Transmissions = []
        };

    internal static void MapPrimitivesTo(this UpdateDialogDto source, DialogEntity destination)
    {
        destination.Progress = source.Progress;
        destination.ExtendedStatus = source.ExtendedStatus;
        destination.ExternalReference = source.ExternalReference;
        destination.DueAt = source.DueAt;
        destination.Process = source.Process;
        destination.PrecedingProcess = source.PrecedingProcess;
        destination.ExpiresAt = source.ExpiresAt;
        destination.IsApiOnly = source.IsApiOnly;
        destination.Content = source.Content.ToDialogContentList(destination.Content) ?? [];
    }

    internal static List<DialogSearchTag> ToDialogSearchTags(this IEnumerable<SearchTagDto> source) =>
        source
            .Select(x => new DialogSearchTag { Value = x.Value })
            .ToList();

    internal static DialogActivity ToDialogActivity(this ActivityDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            CreatedAt = source.CreatedAt ?? default,
            ExtendedType = source.ExtendedType,
            TypeId = source.Type,
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToActor<DialogActivityPerformedByActor>(),
            Description = source.Description.ToLocalizationSet<DialogActivityDescription>()
        };

    internal static DialogTransmission ToDialogTransmission(this TransmissionDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            IdempotentKey = source.IdempotentKey,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            TypeId = source.Type,
            Sender = source.Sender.ToActor<DialogTransmissionSenderActor>(),
            Content = source.Content.ToDialogTransmissionContentList() ?? [],
            Attachments = source.Attachments.Select(x => x.ToDialogTransmissionAttachment()).ToList(),
            NavigationalActions = source.NavigationalActions.Select(x => x.ToDialogTransmissionNavigationalAction()).ToList()
        };

    internal static DialogAttachment ToDialogAttachment(this AttachmentDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Name = source.Name,
            ExpiresAt = source.ExpiresAt,
            DisplayName = source.DisplayName.ToLocalizationSet<AttachmentDisplayName>(),
            Urls = source.Urls.Select(x => x.ToAttachmentUrl()).ToList()
        };

    internal static void UpdateFrom(this DialogAttachment destination, AttachmentDto source)
    {
        destination.Id = source.Id ?? destination.Id;
        destination.Name = source.Name;
        destination.ExpiresAt = source.ExpiresAt;
        destination.DisplayName = source.DisplayName.ToLocalizationSet(destination.DisplayName);
    }

    internal static AttachmentUrl ToAttachmentUrl(this AttachmentUrlDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerTypeId = source.ConsumerType
        };

    internal static void UpdateFrom(this AttachmentUrl destination, AttachmentUrlDto source)
    {
        destination.Id = source.Id ?? destination.Id;
        destination.Url = source.Url;
        destination.MediaType = source.MediaType;
        destination.ConsumerTypeId = source.ConsumerType;
    }

    internal static DialogGuiAction ToDialogGuiAction(this GuiActionDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Action = source.Action,
            Url = source.Url,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsDeleteDialogAction = source.IsDeleteDialogAction,
            PriorityId = source.Priority,
            HttpMethodId = source.HttpMethod ?? HttpVerb.Values.GET,
            Title = source.Title.ToLocalizationSet<DialogGuiActionTitle>(),
            Prompt = source.Prompt.ToLocalizationSet<DialogGuiActionPrompt>()
        };

    internal static void UpdateFrom(this DialogGuiAction destination, GuiActionDto source)
    {
        destination.Id = source.Id ?? destination.Id;
        destination.Action = source.Action;
        destination.Url = source.Url;
        destination.AuthorizationAttribute = source.AuthorizationAttribute;
        destination.IsDeleteDialogAction = source.IsDeleteDialogAction;
        destination.PriorityId = source.Priority;
        destination.HttpMethodId = source.HttpMethod ?? HttpVerb.Values.GET;
        destination.Title = source.Title.ToLocalizationSet(destination.Title);
        destination.Prompt = source.Prompt.ToLocalizationSet(destination.Prompt);
    }

    internal static DialogApiAction ToDialogApiAction(this ApiActionDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Action = source.Action,
            AuthorizationAttribute = source.AuthorizationAttribute,
            Name = source.Name,
            Endpoints = source.Endpoints.Select(x => x.ToDialogApiActionEndpoint()).ToList()
        };

    internal static void UpdateFrom(this DialogApiAction destination, ApiActionDto source)
    {
        destination.Id = source.Id ?? destination.Id;
        destination.Action = source.Action;
        destination.AuthorizationAttribute = source.AuthorizationAttribute;
        destination.Name = source.Name;
    }

    internal static DialogApiActionEndpoint ToDialogApiActionEndpoint(this ApiActionEndpointDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Version = source.Version,
            Url = source.Url,
            HttpMethodId = source.HttpMethod,
            DocumentationUrl = source.DocumentationUrl,
            RequestSchema = source.RequestSchema,
            ResponseSchema = source.ResponseSchema,
            Deprecated = source.Deprecated,
            SunsetAt = source.SunsetAt
        };

    internal static void UpdateFrom(this DialogApiActionEndpoint destination, ApiActionEndpointDto source)
    {
        destination.Id = source.Id ?? destination.Id;
        destination.Version = source.Version;
        destination.Url = source.Url;
        destination.HttpMethodId = source.HttpMethod;
        destination.DocumentationUrl = source.DocumentationUrl;
        destination.RequestSchema = source.RequestSchema;
        destination.ResponseSchema = source.ResponseSchema;
        destination.Deprecated = source.Deprecated;
        destination.SunsetAt = source.SunsetAt;
    }

    private static DialogTransmissionAttachment ToDialogTransmissionAttachment(this TransmissionAttachmentDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Name = source.Name,
            ExpiresAt = source.ExpiresAt,
            DisplayName = source.DisplayName.ToLocalizationSet<AttachmentDisplayName>(),
            Urls = source.Urls.Select(x => x.ToAttachmentUrl()).ToList()
        };

    private static AttachmentUrl ToAttachmentUrl(this TransmissionAttachmentUrlDto source) =>
        new()
        {
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerTypeId = source.ConsumerType
        };

    private static DialogTransmissionNavigationalAction ToDialogTransmissionNavigationalAction(
        this TransmissionNavigationalActionDto source) =>
        new()
        {
            Url = source.Url,
            ExpiresAt = source.ExpiresAt,
            Title = source.Title.ToLocalizationSet<DialogTransmissionNavigationalActionTitle>()!
        };

    private static ContentDto? ToUpdateContentDto(
        this Queries.Get.ContentDto? source) =>
        source is null
            ? null
            : new ContentDto
            {
                Title = source.Title.Copy()!,
                NonSensitiveTitle = source.NonSensitiveTitle.Copy(),
                Summary = source.Summary.Copy(),
                NonSensitiveSummary = source.NonSensitiveSummary.Copy(),
                SenderName = source.SenderName.Copy(),
                AdditionalInfo = source.AdditionalInfo.Copy(),
                ExtendedStatus = source.ExtendedStatus.Copy(),
                MainContentReference = source.MainContentReference.Copy()
            };

    private static ContentDto? ToUpdateContentDto(
        this Features.V1.EndUser.Dialogs.Queries.Get.ContentDto? source) =>
        source is null
            ? null
            : new ContentDto
            {
                Title = source.Title.Copy()!,
                NonSensitiveTitle = null,
                Summary = source.Summary.Copy(),
                NonSensitiveSummary = null,
                SenderName = source.SenderName.Copy(),
                AdditionalInfo = source.AdditionalInfo.Copy(),
                ExtendedStatus = source.ExtendedStatus.Copy(),
                MainContentReference = source.MainContentReference.Copy()
            };

    private static AttachmentDto ToUpdateAttachmentDto(this DialogAttachmentDto source) =>
        new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName.CopyRequired(),
            Name = source.Name,
            Urls = source.Urls.Select(x => new AttachmentUrlDto
            {
                Id = x.Id,
                Url = x.Url,
                MediaType = x.MediaType,
                ConsumerType = x.ConsumerType
            }).ToList(),
            ExpiresAt = source.ExpiresAt
        };

    private static AttachmentDto ToUpdateAttachmentDto(this Features.V1.EndUser.Dialogs.Queries.Get.DialogAttachmentDto source) =>
        new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName.CopyRequired(),
            Name = source.Name,
            Urls = source.Urls.Select(x => new AttachmentUrlDto
            {
                Id = x.Id,
                Url = x.Url,
                MediaType = x.MediaType,
                ConsumerType = x.ConsumerType
            }).ToList(),
            ExpiresAt = source.ExpiresAt
        };

    private static GuiActionDto ToUpdateGuiActionDto(this DialogGuiActionDto source) =>
        new()
        {
            Id = source.Id,
            Action = source.Action,
            Url = source.Url,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsDeleteDialogAction = source.IsDeleteDialogAction,
            HttpMethod = source.HttpMethod,
            Priority = source.Priority,
            Title = source.Title.CopyRequired(),
            Prompt = source.Prompt.CopyOptional()
        };

    private static GuiActionDto ToUpdateGuiActionDto(this Features.V1.EndUser.Dialogs.Queries.Get.DialogGuiActionDto source) =>
        new()
        {
            Id = source.Id,
            Action = source.Action,
            Url = source.Url,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsDeleteDialogAction = source.IsDeleteDialogAction,
            HttpMethod = source.HttpMethod,
            Priority = source.Priority,
            Title = source.Title.CopyRequired(),
            Prompt = source.Prompt.CopyOptional()
        };

    private static ApiActionDto ToUpdateApiActionDto(this DialogApiActionDto source) =>
        new()
        {
            Id = source.Id,
            Action = source.Action,
            AuthorizationAttribute = source.AuthorizationAttribute,
            Name = source.Name,
            Endpoints = source.Endpoints.Select(x => x.ToUpdateApiActionEndpointDto()).ToList()
        };

    private static ApiActionDto ToUpdateApiActionDto(this Features.V1.EndUser.Dialogs.Queries.Get.DialogApiActionDto source) =>
        new()
        {
            Id = source.Id,
            Action = source.Action,
            AuthorizationAttribute = source.AuthorizationAttribute,
            Name = source.Name,
            Endpoints = source.Endpoints.Select(x => x.ToUpdateApiActionEndpointDto()).ToList()
        };

    private static ApiActionEndpointDto ToUpdateApiActionEndpointDto(
        this DialogApiActionEndpointDto source) =>
        new()
        {
            Id = source.Id,
            Version = source.Version,
            Url = source.Url,
            HttpMethod = source.HttpMethod,
            DocumentationUrl = source.DocumentationUrl,
            RequestSchema = source.RequestSchema,
            ResponseSchema = source.ResponseSchema,
            Deprecated = source.Deprecated,
            SunsetAt = source.SunsetAt
        };

    private static ApiActionEndpointDto ToUpdateApiActionEndpointDto(
        this Features.V1.EndUser.Dialogs.Queries.Get.DialogApiActionEndpointDto source) =>
        new()
        {
            Id = source.Id,
            Version = source.Version,
            Url = source.Url,
            HttpMethod = source.HttpMethod,
            DocumentationUrl = source.DocumentationUrl,
            RequestSchema = source.RequestSchema,
            ResponseSchema = source.ResponseSchema,
            Deprecated = source.Deprecated,
            SunsetAt = source.SunsetAt
        };

    private static DialogStatusInput ToDialogStatusInput(this DialogStatus.Values source) =>
        (DialogStatusInput)source;

    private static ContentValueDto? Copy(this ContentValueDto? source) =>
        source is null
            ? null
            : new ContentValueDto
            {
                MediaType = source.MediaType,
                IsAuthorized = source.IsAuthorized,
                Value = source.Value.CopyRequired()
            };

    private static List<LocalizationDto> CopyRequired(this IEnumerable<LocalizationDto> source) =>
        source.Select(x => new LocalizationDto { LanguageCode = x.LanguageCode, Value = x.Value }).ToList();

    private static List<LocalizationDto>? CopyOptional(this IEnumerable<LocalizationDto>? source) =>
        source?.Select(x => new LocalizationDto { LanguageCode = x.LanguageCode, Value = x.Value }).ToList();
}
