#pragma warning disable CS8601

#pragma warning disable CS0618

using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;

internal static class DialogMapper
{
    extension(DialogEntity source)
    {
        public DialogDto ToDto() => new()
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
                .First(label => SystemLabel.IsDefaultArchiveBinGroup(label.SystemLabelId))
                .SystemLabelId,
            IsApiOnly = source.IsApiOnly,
            HasUnopenedContent = source.HasUnopenedContent,
            Content = source.Content.ToDialogContentDto<ContentDto>(),
            FromServiceOwnerTransmissionsCount = source.FromServiceOwnerTransmissionsCount,
            FromPartyTransmissionsCount = source.FromPartyTransmissionsCount,
            SearchTags = source.SearchTags
                .Select(tag => tag.ToDto())
                .ToList(),
            Attachments = source.Attachments
                .Select(attachment => attachment.ToDto())
                .ToList(),
            Transmissions = source.Transmissions
                .Select(transmission => transmission.ToDto())
                .ToList(),
            GuiActions = source.GuiActions
                .Select(action => action.ToDto())
                .ToList(),
            ApiActions = source.ApiActions
                .Select(action => action.ToDto())
                .ToList(),
            Activities = source.Activities
                .Select(activity => activity.ToDto())
                .ToList(),
            ServiceOwnerContext = source.ServiceOwnerContext.ToDto(),
            EndUserContext = source.EndUserContext.ToDto()
        };
    }

    extension(DialogEndUserContext source)
    {
        public DialogEndUserContextDto ToDto() => new()
        {
            Revision = source.Revision,
            SystemLabels = source.DialogEndUserContextSystemLabels
                .Select(label => label.SystemLabelId)
                .ToList()
        };
    }

    extension(DialogServiceOwnerContext source)
    {
        public DialogServiceOwnerContextDto ToDto() => new()
        {
            Revision = source.Revision,
            ServiceOwnerLabels = source.ServiceOwnerLabels
                .Select(label => label.ToDto())
                .ToList()
        };
    }

    extension(DialogServiceOwnerLabel source)
    {
        public DialogServiceOwnerLabelDto ToDto() => new()
        {
            Value = source.Value
        };
    }

    extension(DialogSeenLog source)
    {
        public DialogSeenLogDto ToDto() => new()
        {
            Id = source.Id,
            SeenAt = source.CreatedAt,
            SeenBy = source.SeenBy.ToDto(),
            IsViaServiceOwner = source.IsViaServiceOwner
        };
    }

    extension(DialogActivity source)
    {
        public DialogActivityDto ToDto() => new()
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            ExtendedType = source.ExtendedType,
            Type = source.TypeId,
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToDto(),
            Description = source.Description.ToDto()
        };
    }

    extension(DialogApiAction source)
    {
        public DialogApiActionDto ToDto() => new()
        {
            Id = source.Id,
            Action = source.Action,
            AuthorizationAttribute = source.AuthorizationAttribute,
            Name = source.Name,
            Endpoints = source.Endpoints
                .Select(endpoint => endpoint.ToDto())
                .ToList()
        };
    }

    extension(DialogApiActionEndpoint source)
    {
        public DialogApiActionEndpointDto ToDto() => new()
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

    extension(DialogGuiAction source)
    {
        public DialogGuiActionDto ToDto() => new()
        {
            Id = source.Id,
            Action = source.Action,
            Url = source.Url,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsDeleteDialogAction = source.IsDeleteDialogAction,
            Priority = source.PriorityId,
            HttpMethod = source.HttpMethodId,
            Title = source.Title.ToDto() ?? [],
            Prompt = source.Prompt.ToDto()
        };
    }

    extension(DialogAttachment source)
    {
        public DialogAttachmentDto ToDto() => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName.ToDto() ?? [],
            Name = source.Name,
            Urls = source.Urls
                .Select(url => url.ToDialogAttachmentUrlDto())
                .ToList(),
            ExpiresAt = source.ExpiresAt
        };
    }

    extension(AttachmentUrl source)
    {
        public DialogAttachmentUrlDto ToDialogAttachmentUrlDto() => new()
        {
            Id = source.Id,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerType = source.ConsumerTypeId
        };

        public DialogTransmissionAttachmentUrlDto ToDialogTransmissionAttachmentUrlDto() => new()
        {
            Id = source.Id,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerType = source.ConsumerTypeId
        };
    }

    extension(DialogSearchTag source)
    {
        public SearchTagDto ToDto() => new()
        {
            Value = source.Value
        };
    }

    extension(DialogTransmission source)
    {
        public DialogTransmissionDto ToDto() => new()
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
            Content = source.Content.ToDialogTransmissionContentDto<DialogTransmissionContentDto>()!,
            IsOpened = DialogUnopenedContent.IsOpened(source),
            Attachments = source.Attachments
                .Select(attachment => attachment.ToDto())
                .ToList(),
            NavigationalActions = source.NavigationalActions
                .Select(action => action.ToDto())
                .ToList()
        };
    }

    extension(DialogTransmissionAttachment source)
    {
        public DialogTransmissionAttachmentDto ToDto() => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName.ToDto() ?? [],
            Name = source.Name,
            Urls = source.Urls
                .Select(url => url.ToDialogTransmissionAttachmentUrlDto())
                .ToList(),
            ExpiresAt = source.ExpiresAt
        };
    }

    extension(DialogTransmissionNavigationalAction source)
    {
        public DialogTransmissionNavigationalActionDto ToDto() => new()
        {
            Title = source.Title.ToDto() ?? [],
            Url = source.Url,
            ExpiresAt = source.ExpiresAt
        };
    }
}

#pragma warning restore CS8601
#pragma warning restore CS0618
