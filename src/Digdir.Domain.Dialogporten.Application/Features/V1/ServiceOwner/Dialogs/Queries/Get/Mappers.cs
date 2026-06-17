using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

internal static class DialogMapExtensions
{
    extension(DialogEntity source)
    {
        internal DialogDto ToDto() => new()
        {
            Id = source.Id,
            IdempotentKey = source.IdempotentKey,
            Revision = source.Revision,
            Org = source.Org,
            ServiceResource = source.ServiceResource,
            ServiceResourceType = source.ServiceResourceType,
            Party = source.Party,
            Progress = source.Progress,
            Process = source.Process,
            PrecedingProcess = source.PrecedingProcess,
            ExtendedStatus = source.ExtendedStatus,
            ExternalReference = source.ExternalReference,
            DeletedAt = source.DeletedAt,
            VisibleFrom = source.VisibleFrom,
            DueAt = source.DueAt,
            ExpiresAt = source.ExpiresAt,
            CreatedAt = source.CreatedAt,
            UpdatedAt = source.UpdatedAt,
            ContentUpdatedAt = source.ContentUpdatedAt,
            Status = source.StatusId,
            SystemLabel = source.EndUserContext.DialogEndUserContextSystemLabels
                .First(l => SystemLabel.IsDefaultArchiveBinGroup(l.SystemLabelId))
                .SystemLabelId,
            IsApiOnly = source.IsApiOnly,
            HasUnopenedContent = source.HasUnopenedContent,
            Content = source.Content.ToContentDto(),
            FromServiceOwnerTransmissionsCount = source.FromServiceOwnerTransmissionsCount,
            FromPartyTransmissionsCount = source.FromPartyTransmissionsCount,
            SearchTags = source.SearchTags.ToDtoList(x => x.ToDto()),
            Attachments = source.Attachments.ToDtoList(x => x.ToDto()),
            Transmissions = source.Transmissions.ToDtoList(x => x.ToDto()),
            GuiActions = source.GuiActions.ToDtoList(x => x.ToDto()),
            ApiActions = source.ApiActions.ToDtoList(x => x.ToDto()),
            Activities = source.Activities.ToDtoList(x => x.ToDto()),
            IsContentSeen = source.IsSeenSinceLastContentUpdate && !source.IsMarkedAsUnopened(),
            ServiceOwnerContext = source.ServiceOwnerContext.ToDto(),
            EndUserContext = source.EndUserContext.ToDto()
        };
    }
}

internal static class DialogEndUserContextMapExtensions
{
    extension(DialogEndUserContext source)
    {
        internal DialogEndUserContextDto ToDto() => new()
        {
            Revision = source.Revision,
            SystemLabels = source.DialogEndUserContextSystemLabels
                .Select(x => x.SystemLabelId)
                .ToList()
        };
    }
}

internal static class DialogServiceOwnerContextMapExtensions
{
    extension(DialogServiceOwnerContext source)
    {
        internal DialogServiceOwnerContextDto ToDto() => new()
        {
            Revision = source.Revision,
            ServiceOwnerLabels = source.ServiceOwnerLabels.ToDtoList(x => x.ToDto())
        };
    }
}

internal static class DialogServiceOwnerLabelMapExtensions
{
    extension(DialogServiceOwnerLabel source)
    {
        internal DialogServiceOwnerLabelDto ToDto() => new()
        {
            Value = source.Value
        };
    }
}

internal static class DialogContentListMapExtensions
{
    extension(List<DialogContent>? sources)
    {
        internal ContentDto? ToContentDto()
        {
            if (sources is null || sources.Count == 0)
            {
                return null;
            }

            return sources.Aggregate(new ContentDto(), (dto, content) =>
            {
                switch (content.TypeId)
                {
                    case DialogContentType.Values.Title:
                        dto.Title = content.ToContentValueDto();
                        return dto;
                    case DialogContentType.Values.NonSensitiveTitle:
                        dto.NonSensitiveTitle = content.ToContentValueDto();
                        return dto;
                    case DialogContentType.Values.SenderName:
                        dto.SenderName = content.ToContentValueDto();
                        return dto;
                    case DialogContentType.Values.Summary:
                        dto.Summary = content.ToContentValueDto();
                        return dto;
                    case DialogContentType.Values.NonSensitiveSummary:
                        dto.NonSensitiveSummary = content.ToContentValueDto();
                        return dto;
                    case DialogContentType.Values.AdditionalInfo:
                        dto.AdditionalInfo = content.ToContentValueDto();
                        return dto;
                    case DialogContentType.Values.ExtendedStatus:
                        dto.ExtendedStatus = content.ToContentValueDto();
                        return dto;
                    case DialogContentType.Values.MainContentReference:
                        dto.MainContentReference = content.ToContentValueDto();
                        return dto;
                    default:
                        throw new InvalidOperationException(
                            $"Unknown TypeId {content.TypeId} found in DialogContent {content.Id}");
                }
            });
        }
    }
}

internal static class DialogSeenLogMapExtensions
{
    extension(DialogSeenLog source)
    {
        internal DialogSeenLogDto ToDto(string? endUserId = null)
        {
            var actorId = source.SeenBy.ActorNameEntity?.ActorId;
            return new()
            {
                Id = source.Id,
                SeenAt = source.CreatedAt,
                SeenBy = source.SeenBy.ToDto(),
                IsViaServiceOwner = source.IsViaServiceOwner,
                IsCurrentEndUser = endUserId == actorId
            };
        }
    }
}

internal static class DialogActivityMapExtensions
{
    extension(DialogActivity source)
    {
        internal DialogActivityDto ToDto() => new()
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            ExtendedType = source.ExtendedType,
            Type = source.TypeId,
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToDto(),
            Description = source.Description.ToDtoList()!
        };
    }
}

internal static class DialogApiActionMapExtensions
{
    extension(DialogApiAction source)
    {
        internal DialogApiActionDto ToDto() => new()
        {
            Id = source.Id,
            Action = source.Action,
            AuthorizationAttribute = source.AuthorizationAttribute,
            Name = source.Name,
            Endpoints = source.Endpoints.ToDtoList(x => x.ToDto())
        };
    }
}

internal static class DialogApiActionEndpointMapExtensions
{
    extension(DialogApiActionEndpoint source)
    {
        internal DialogApiActionEndpointDto ToDto() => new()
        {
            Id = source.Id,
            Version = source.Version,
            Url = source.Url,
            HttpMethod = source.HttpMethodId,
            DocumentationUrl = source.DocumentationUrl,
            RequestSchema = source.RequestSchema,
            ResponseSchema = source.ResponseSchema,
            Deprecated = source.Deprecated,
            SunsetAt = source.SunsetAt
        };
    }
}

internal static class DialogGuiActionMapExtensions
{
    extension(DialogGuiAction source)
    {
        internal DialogGuiActionDto ToDto() => new()
        {
            Id = source.Id,
            Action = source.Action,
            Url = source.Url,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsDeleteDialogAction = source.IsDeleteDialogAction,
            Priority = source.PriorityId,
            HttpMethod = source.HttpMethodId,
            Title = source.Title.ToDtoList()!,
            Prompt = source.Prompt.ToDtoList()
        };
    }
}

internal static class DialogAttachmentMapExtensions
{
    extension(DialogAttachment source)
    {
        internal DialogAttachmentDto ToDto() => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName.ToDtoList()!,
            Name = source.Name,
            Urls = source.Urls.ToDtoList(x => x.ToDialogAttachmentUrlDto()),
            ExpiresAt = source.ExpiresAt
        };
    }
}

internal static class DialogAttachmentUrlMapExtensions
{
    extension(AttachmentUrl source)
    {
        internal DialogAttachmentUrlDto ToDialogAttachmentUrlDto() => new()
        {
            Id = source.Id,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerType = source.ConsumerTypeId
        };

        internal DialogTransmissionAttachmentUrlDto ToDialogTransmissionAttachmentUrlDto() => new()
        {
            Id = source.Id,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerType = source.ConsumerTypeId
        };
    }
}

internal static class DialogSearchTagMapExtensions
{
    extension(DialogSearchTag source)
    {
        internal SearchTagDto ToDto() => new()
        {
            Value = source.Value
        };
    }
}

internal static class DialogTransmissionMapExtensions
{
    extension(DialogTransmission source)
    {
        internal DialogTransmissionDto ToDto() => new()
        {
            Id = source.Id,
            IdempotentKey = source.IdempotentKey,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            Type = source.TypeId,
            Sender = source.Sender.ToDto(),
            Content = source.Content.ToTransmissionContentDto<DialogTransmissionContentDto>()!,
            IsOpened = DialogUnopenedContent.IsOpened(source),
            Attachments = source.Attachments.ToDtoList(x => x.ToDto()),
            NavigationalActions = source.NavigationalActions.ToDtoList(x => x.ToDto())
        };
    }
}

internal static class DialogTransmissionAttachmentMapExtensions
{
    extension(DialogTransmissionAttachment source)
    {
        internal DialogTransmissionAttachmentDto ToDto() => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName.ToDtoList()!,
            Name = source.Name,
            Urls = source.Urls.ToDtoList(x => x.ToDialogTransmissionAttachmentUrlDto()),
            ExpiresAt = source.ExpiresAt
        };
    }
}

internal static class DialogTransmissionNavigationalActionMapExtensions
{
    extension(DialogTransmissionNavigationalAction source)
    {
        internal DialogTransmissionNavigationalActionDto ToDto() => new()
        {
            Title = source.Title.ToDtoList()!,
            Url = source.Url,
            ExpiresAt = source.ExpiresAt
        };
    }
}

internal static class NullableCollectionMapExtensions
{
    extension<TSource>(IEnumerable<TSource>? source)
    {
        internal List<TDestination> ToDtoList<TDestination>(Func<TSource, TDestination> map) =>
            source?.Select(map).ToList()!;
    }
}
