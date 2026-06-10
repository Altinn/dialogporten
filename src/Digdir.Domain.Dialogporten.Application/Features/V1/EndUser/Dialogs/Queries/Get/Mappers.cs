using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;

internal static class DialogMapExtensions
{
    extension(DialogEntity source)
    {
        internal DialogDto ToDto() => new()
        {
            Id = source.Id,
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
            Content = source.Content.ToContentDto()!,
            FromServiceOwnerTransmissionsCount = source.FromServiceOwnerTransmissionsCount,
            FromPartyTransmissionsCount = source.FromPartyTransmissionsCount,
            Attachments = source.Attachments.Select(a => a.ToDto()).ToList(),
            Transmissions = source.Transmissions.Select(t => t.ToDto()).ToList(),
            GuiActions = source.GuiActions.Select(g => g.ToDto()).ToList(),
            ApiActions = source.ApiActions.Select(a => a.ToDto()).ToList(),
            Activities = source.Activities.Select(a => a.ToDto()).ToList(),
            IsContentSeen = source.IsSeenSinceLastContentUpdate && !source.IsMarkedAsUnopened(),
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

            var dto = new ContentDto();

            foreach (var content in sources)
            {
                switch (content.TypeId)
                {
                    case DialogContentType.Values.Title:
                        dto.Title = content.ToContentValueDto();
                        break;
                    case DialogContentType.Values.NonSensitiveTitle:
                        break;
                    case DialogContentType.Values.SenderName:
                        dto.SenderName = content.ToContentValueDto();
                        break;
                    case DialogContentType.Values.Summary:
                        dto.Summary = content.ToContentValueDto();
                        break;
                    case DialogContentType.Values.NonSensitiveSummary:
                        break;
                    case DialogContentType.Values.AdditionalInfo:
                        dto.AdditionalInfo = content.ToContentValueDto();
                        break;
                    case DialogContentType.Values.ExtendedStatus:
                        dto.ExtendedStatus = content.ToContentValueDto();
                        break;
                    case DialogContentType.Values.MainContentReference:
                        dto.MainContentReference = content.ToContentValueDto();
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Unknown TypeId {content.TypeId} found in DialogContent {content.Id}");
                }
            }

            return dto;
        }
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
            Urls = source.Urls.Select(u => u.ToDialogAttachmentUrlDto()).ToList(),
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
            Endpoints = source.Endpoints.Select(e => e.ToDto()).ToList()
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

internal static class DialogTransmissionMapExtensions
{
    extension(DialogTransmission source)
    {
        internal DialogTransmissionDto ToDto() => new()
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            Type = source.TypeId,
            Sender = source.Sender.ToDto(),
            IsOpened = DialogUnopenedContent.IsOpened(source),
            Content = source.Content.ToTransmissionContentDto<DialogTransmissionContentDto>()!,
            Attachments = source.Attachments.Select(a => a.ToDto()).ToList(),
            NavigationalActions = source.NavigationalActions.Select(n => n.ToDto()).ToList()
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
            Urls = source.Urls.Select(u => u.ToDialogTransmissionAttachmentUrlDto()).ToList(),
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

internal static class DialogSeenLogMapExtensions
{
    extension(DialogSeenLog source)
    {
        internal DialogSeenLogDto ToDto() => new()
        {
            Id = source.Id,
            SeenAt = source.CreatedAt,
            SeenBy = source.SeenBy.ToDto(),
            IsViaServiceOwner = source.IsViaServiceOwner
        };
    }
}
