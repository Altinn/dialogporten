using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Domain.Dialogporten.Domain.Localizations;
using GetDialogDtoEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.DialogDto;
using GetDialogDtoSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.DialogDto;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

public static class Mappers
{
    public static UpdateDialogDto ToUpdateDialogDto(this GetDialogDtoSO source) =>
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

    public static UpdateDialogDto ToUpdateDialogDto(this GetDialogDtoEU source) =>
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
            Attachments = source.Attachments.Select(x => x.ToUpdateAttachmentDto()).ToList(),
            GuiActions = source.GuiActions.Select(x => x.ToUpdateGuiActionDto()).ToList(),
            ApiActions = source.ApiActions.Select(x => x.ToUpdateApiActionDto()).ToList(),
            Activities = [],
            Transmissions = []
        };

    internal static void MapTo(this UpdateDialogDto source, DialogEntity destination)
    {
        destination.Progress = source.Progress;
        destination.ExtendedStatus = source.ExtendedStatus;
        destination.ExternalReference = source.ExternalReference;
        destination.DueAt = source.DueAt;
        destination.Process = source.Process;
        destination.PrecedingProcess = source.PrecedingProcess;
        destination.ExpiresAt = source.ExpiresAt;
        destination.IsApiOnly = source.IsApiOnly;
        destination.Content = SyncDialogContent(source.Content, destination.Content) ?? [];
    }

    internal static List<DialogSearchTag> ToDialogSearchTags(this IEnumerable<SearchTagDto> source) =>
        source
            .Select(x => new DialogSearchTag { Value = x.Value })
            .ToList();

    internal static DialogActivity ToDialogActivity(this ActivityDto source) =>
        new()
        {
            Id = source.Id ?? default,
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
            Id = source.Id ?? default,
            IdempotentKey = source.IdempotentKey,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            TypeId = source.Type,
            Sender = source.Sender.ToActor<DialogTransmissionSenderActor>(),
            Content = SyncTransmissionContent(source.Content, []) ?? [],
            Attachments = source.Attachments.Select(x => x.ToDialogTransmissionAttachment()).ToList(),
            NavigationalActions = source.NavigationalActions.Select(x => x.ToDialogTransmissionNavigationalAction()).ToList()
        };

    internal static DialogAttachment ToDialogAttachment(this AttachmentDto source) =>
        new()
        {
            Id = source.Id ?? default,
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
            Id = source.Id ?? default,
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
            Id = source.Id ?? default,
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
            Id = source.Id ?? default,
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
            Id = source.Id ?? default,
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
            Id = source.Id ?? default,
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

    private static Update.ContentDto? ToUpdateContentDto(
        this Queries.Get.ContentDto? source) =>
        source is null
            ? null
            : new Update.ContentDto
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

    private static Update.ContentDto? ToUpdateContentDto(
        this Features.V1.EndUser.Dialogs.Queries.Get.ContentDto? source) =>
        source is null
            ? null
            : new Update.ContentDto
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

    private static AttachmentDto ToUpdateAttachmentDto(this Queries.Get.DialogAttachmentDto source) =>
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

    private static GuiActionDto ToUpdateGuiActionDto(this Queries.Get.DialogGuiActionDto source) =>
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

    private static ApiActionDto ToUpdateApiActionDto(this Queries.Get.DialogApiActionDto source) =>
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
        this Queries.Get.DialogApiActionEndpointDto source) =>
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

    private static List<DialogContent>? SyncDialogContent(ContentDto? source, List<DialogContent>? destinations)
    {
        if (source is null)
        {
            return null;
        }

        destinations ??= [];
        foreach (var contentType in DialogContentType.GetValues())
        {
            var sourceValue = contentType.Id switch
            {
                DialogContentType.Values.Title => source.Title,
                DialogContentType.Values.NonSensitiveTitle => source.NonSensitiveTitle,
                DialogContentType.Values.Summary => source.Summary,
                DialogContentType.Values.NonSensitiveSummary => source.NonSensitiveSummary,
                DialogContentType.Values.SenderName => source.SenderName,
                DialogContentType.Values.AdditionalInfo => source.AdditionalInfo,
                DialogContentType.Values.ExtendedStatus => source.ExtendedStatus,
                DialogContentType.Values.MainContentReference => source.MainContentReference,
                _ => throw new InvalidOperationException($"Unknown {nameof(DialogContentType)} '{contentType.Id}'")
            };

            SyncContent(
                destinations,
                contentType.Id,
                sourceValue,
                static (typeId, mediaType, localizations) => new DialogContent
                {
                    TypeId = typeId,
                    MediaType = mediaType,
                    Value = new DialogContentValue { Localizations = localizations }
                });
        }

        return destinations;
    }

    private static List<DialogTransmissionContent>? SyncTransmissionContent(
        TransmissionContentDto? source,
        List<DialogTransmissionContent>? destinations)
    {
        if (source is null)
        {
            return null;
        }

        destinations ??= [];
        foreach (var contentType in DialogTransmissionContentType.GetValues())
        {
            var sourceValue = contentType.Id switch
            {
                DialogTransmissionContentType.Values.Title => source.Title,
                DialogTransmissionContentType.Values.Summary => source.Summary,
                DialogTransmissionContentType.Values.ContentReference => source.ContentReference,
                _ => throw new InvalidOperationException(
                    $"Unknown {nameof(DialogTransmissionContentType)} '{contentType.Id}'")
            };

            SyncContent(
                destinations,
                contentType.Id,
                sourceValue,
                static (typeId, mediaType, localizations) => new DialogTransmissionContent
                {
                    TypeId = typeId,
                    MediaType = mediaType,
                    Value = new DialogTransmissionContentValue { Localizations = localizations }
                });
        }

        return destinations;
    }

    private static void SyncContent<TContent, TType>(
        List<TContent> destinations,
        TType typeId,
        ContentValueDto? sourceValue,
        Func<TType, string, List<Localization>, TContent> create)
        where TContent : class
    {
        var existing = destinations.FirstOrDefault(x => GetTypeId(x)!.Equals(typeId));

        if (sourceValue is null)
        {
            if (existing is not null)
            {
                destinations.Remove(existing);
            }

            return;
        }

        var mediaType = sourceValue.MediaType.MapDeprecatedMediaType();
        if (existing is not null)
        {
            SetContent(existing, mediaType, sourceValue.Value);
            return;
        }

        destinations.Add(create(typeId, mediaType, sourceValue.Value.Select(x => x.ToLocalization()).ToList()));
    }

    private static object? GetTypeId(object content) =>
        content switch
        {
            DialogContent dialogContent => dialogContent.TypeId,
            DialogTransmissionContent transmissionContent => transmissionContent.TypeId,
            _ => throw new ArgumentOutOfRangeException(nameof(content), content, null)
        };

    private static void SetContent(object content, string mediaType, ICollection<LocalizationDto> localizations)
    {
        _ = content switch
        {
            DialogContent dialogContent => UpdateDialogContent(dialogContent, mediaType, localizations),
            DialogTransmissionContent transmissionContent => UpdateTransmissionContent(
                transmissionContent,
                mediaType,
                localizations),
            _ => throw new ArgumentOutOfRangeException(nameof(content), content, null)
        };
    }

    private static bool UpdateDialogContent(
        DialogContent content,
        string mediaType,
        ICollection<LocalizationDto> localizations)
    {
        content.MediaType = mediaType;
        content.Value.Localizations.MergeFrom(localizations);
        return true;
    }

    private static bool UpdateTransmissionContent(
        DialogTransmissionContent content,
        string mediaType,
        ICollection<LocalizationDto> localizations)
    {
        content.MediaType = mediaType;
        content.Value.Localizations.MergeFrom(localizations);
        return true;
    }

    private static DialogStatusInput ToDialogStatusInput(this DialogStatus.Values source) =>
        (DialogStatusInput)source;

    private static TLocalizationSet? ToLocalizationSet<TLocalizationSet>(
        this IEnumerable<LocalizationDto>? source,
        TLocalizationSet? destination = null)
        where TLocalizationSet : LocalizationSet, new()
    {
        var localizations = source as ICollection<LocalizationDto> ?? source?.ToList();
        if (localizations is null || localizations.Count == 0)
        {
            return null;
        }

        destination ??= new TLocalizationSet();
        destination.Localizations.MergeFrom(localizations);
        return destination;
    }

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
