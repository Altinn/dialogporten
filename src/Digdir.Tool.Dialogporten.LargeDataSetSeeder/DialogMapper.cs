using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Actor = Digdir.Domain.Dialogporten.Domain.Actors.Actor;
using DialogActivity = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities.DialogActivity;
using DialogContent = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents.DialogContent;
using DialogGuiAction = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions.DialogGuiAction;
using DialogSearchTag = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogSearchTag;
using DialogTransmission = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.DialogTransmission;
using Localization = Digdir.Domain.Dialogporten.Domain.Localizations.Localization;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder;

internal static class DialogMapper
{
    public static CreateDialogDto ToCreateDto(this DialogEntity dialog) =>
        new()
        {
            Id = dialog.Id,
            DueAt = dialog.DueAt,
            ExpiresAt = dialog.ExpiresAt,
            ExtendedStatus = dialog.ExtendedStatus,
            ExternalReference = dialog.ExternalReference,
            IsApiOnly = dialog.IsApiOnly,
            Party = dialog.Party,
            Process = dialog.Process,
            PrecedingProcess = dialog.PrecedingProcess,
            ServiceResource = dialog.ServiceResource,
            CreatedAt = dialog.CreatedAt,
            VisibleFrom = dialog.VisibleFrom,
            IdempotentKey = dialog.IdempotentKey,
            Progress = dialog.Progress,
            Status = (DialogStatusInput)dialog.StatusId,
            SystemLabel = dialog.EndUserContext.DialogEndUserContextSystemLabels
                .Select(x => x.SystemLabelId)
                .Single(SystemLabel.IsDefaultArchiveBinGroup),
            UpdatedAt = dialog.UpdatedAt,
            ServiceOwnerContext = dialog.ServiceOwnerContext.ToDto(),
            Content = dialog.Content.ToDto(),
            Activities = dialog.Activities.ToDtos(),
            Attachments = dialog.Attachments.ToDtos(),
            GuiActions = dialog.GuiActions.ToDtos(),
            ApiActions = dialog.ApiActions.ToDtos(),
            Transmissions = dialog.Transmissions.ToDtos(),
            SearchTags = dialog.SearchTags.ToDtos(),
        };

    private static DialogServiceOwnerContextDto? ToDto(this DialogServiceOwnerContext? context) =>
        context is null
            ? null
            : new DialogServiceOwnerContextDto
            {
                ServiceOwnerLabels = context.ServiceOwnerLabels
                    .Select(x => new ServiceOwnerLabelDto
                    {
                        Value = x.Value
                    })
                    .ToList()
            };

    private static List<SearchTagDto> ToDtos(this List<DialogSearchTag> searchTags) => searchTags
        .Select(x => new SearchTagDto { Value = x.Value })
        .ToList();

    private static List<TransmissionDto> ToDtos(this List<DialogTransmission> transmissions) =>
        transmissions.Select(x => new TransmissionDto
        {
            Id = x.Id,
            CreatedAt = x.CreatedAt,
            Type = x.TypeId,
            AuthorizationAttribute = x.AuthorizationAttribute,
            ExtendedType = x.ExtendedType,
            ExternalReference = x.ExternalReference,
            Sender = x.Sender.ToDto(),
            RelatedTransmissionId = x.RelatedTransmissionId,
            Content = x.Content.ToDto(),
            Attachments = x.Attachments.Select(x => new TransmissionAttachmentDto()
            {
                Id = x.Id,
                DisplayName = x.DisplayName!.Localizations.ToDtos(),
                Urls = x.Urls.Select(x => new TransmissionAttachmentUrlDto
                {
                    MediaType = x.MediaType,
                    Url = x.Url,
                    ConsumerType = x.ConsumerTypeId
                }).ToList()
            }).ToList(),
        }).ToList();

    private static List<ActivityDto> ToDtos(this List<DialogActivity> activities) => activities
        .Select(x => new ActivityDto
        {
            Type = x.TypeId,
            CreatedAt = x.CreatedAt,
            Id = x.Id,
            ExtendedType = x.ExtendedType,
            TransmissionId = x.TransmissionId,
            PerformedBy = x.PerformedBy.ToDto(),
            Description = x.Description?.Localizations.ToDtos() ?? []
        })
        .ToList();

    private static ActorDto ToDto(this Actor actor) =>
        new()
        {
            ActorId = actor.ActorNameEntity?.ActorId,
            ActorName = null, // cannot set name and id, so we leave name null
            ActorType = actor.ActorTypeId
        };

    private static List<AttachmentDto> ToDtos(this List<DialogAttachment> attachments) => attachments
        .Select(x => new AttachmentDto
        {
            Id = x.Id,
            DisplayName = x.DisplayName!.Localizations.ToDtos(),
            Urls = x.Urls.Select(x => new AttachmentUrlDto
            {
                ConsumerType = x.ConsumerTypeId,
                MediaType = x.MediaType,
                Url = x.Url
            }).ToList()
        })
        .ToList();

    private static List<GuiActionDto> ToDtos(this List<DialogGuiAction> guiActions) =>
        guiActions
            .Select(x => new GuiActionDto()
            {
                Id = x.Id,
                IsDeleteDialogAction = x.IsDeleteDialogAction,
                Action = x.Action,
                AuthorizationAttribute = x.AuthorizationAttribute,
                HttpMethod = x.HttpMethodId,
                Priority = x.PriorityId,
                Prompt = x.Prompt?.Localizations.ToDtos(),
                Title = x.Title!.Localizations.ToDtos(),
                Url = x.Url
            }).ToList();

    private static List<ApiActionDto> ToDtos(this List<DialogApiAction> _) => [];

    private static List<LocalizationDto> ToDtos(this List<Localization> localizations) => localizations
        .Select(x => new LocalizationDto
        {
            LanguageCode = x.LanguageCode,
            Value = x.Value
        })
        .ToList();

    private static ContentDto ToDto(this List<DialogContent> content) =>
        new()
        {
            Title = new ContentValueDto
            {
                Value = content
                    .Single(x => x.TypeId == DialogContentType.Values.Title).Value.Localizations
                    .Select(x => new LocalizationDto
                    {
                        LanguageCode = x.LanguageCode,
                        Value = x.Value
                    })
                    .ToList(),
                MediaType = "text/plain"
            },
            Summary = new ContentValueDto
            {
                Value = content
                    .Single(x => x.TypeId == DialogContentType.Values.Summary).Value.Localizations
                    .Select(x => new LocalizationDto
                    {
                        LanguageCode = x.LanguageCode,
                        Value = x.Value
                    })
                    .ToList(),
                MediaType = "text/plain"
            }
        };

    private static TransmissionContentDto ToDto(this List<DialogTransmissionContent> content) =>
        new()
        {
            Title = new ContentValueDto
            {
                Value = content
                    .Single(x => x.TypeId == DialogTransmissionContentType.Values.Title).Value.Localizations
                    .Select(x => new LocalizationDto
                    {
                        LanguageCode = x.LanguageCode,
                        Value = x.Value
                    })
                    .ToList(),
                MediaType = "text/plain"
            },
            Summary = new ContentValueDto
            {
                Value = content
                    .Single(x => x.TypeId == DialogTransmissionContentType.Values.Summary).Value.Localizations
                    .Select(x => new LocalizationDto
                    {
                        LanguageCode = x.LanguageCode,
                        Value = x.Value
                    })
                    .ToList(),
                MediaType = "text/plain"
            }
        };
}
