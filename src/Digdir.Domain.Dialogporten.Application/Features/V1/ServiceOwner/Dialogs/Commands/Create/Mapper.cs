using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

internal static class DialogMapper
{
    extension(CreateDialogDto source)
    {
        public DialogEntity ToEntity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            IdempotentKey = source.IdempotentKey,
            ServiceResource = source.ServiceResource,
            Party = source.Party,
            Progress = source.Progress,
            ExtendedStatus = source.ExtendedStatus,
            ExternalReference = source.ExternalReference,
            VisibleFrom = source.VisibleFrom,
            DueAt = source.DueAt,
            Process = source.Process,
            PrecedingProcess = source.PrecedingProcess,
            ExpiresAt = source.ExpiresAt,
            IsApiOnly = source.IsApiOnly,
            CreatedAt = source.CreatedAt ?? default,
            UpdatedAt = source.UpdatedAt ?? default,
            StatusId = (source.Status ?? DialogStatusInput.NotApplicable).ToEntity(),
            Content = source.Content.ToDialogContentEntities() ?? [],
            SearchTags = source.SearchTags
                .Select(tag => tag.ToEntity())
                .ToList(),
            Attachments = source.Attachments
                .Select(attachment => attachment.ToEntity())
                .ToList(),
            Transmissions = source.Transmissions
                .Select(transmission => transmission.ToEntity())
                .ToList(),
            GuiActions = source.GuiActions
                .Select(action => action.ToEntity())
                .ToList(),
            ApiActions = source.ApiActions
                .Select(action => action.ToEntity())
                .ToList(),
            Activities = source.Activities
                .Select(activity => activity.ToEntity())
                .ToList()
        };
    }

    extension(SearchTagDto source)
    {
        public DialogSearchTag ToEntity() => new()
        {
            Value = source.Value
        };
    }

    extension(ServiceOwnerLabelDto source)
    {
        public Domain.DialogServiceOwnerContexts.Entities.DialogServiceOwnerLabel ToEntity() => new()
        {
            Value = source.Value
        };
    }

    extension(AttachmentDto source)
    {
        public DialogAttachment ToEntity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            DisplayName = source.DisplayName.MergeInto<AttachmentDisplayName>(null),
            Name = source.Name,
            Urls = source.Urls
                .Select(url => url.ToEntity())
                .ToList(),
            ExpiresAt = source.ExpiresAt
        };
    }

    extension(AttachmentUrlDto source)
    {
        public AttachmentUrl ToEntity() => new()
        {
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerTypeId = source.ConsumerType
        };
    }

    extension(GuiActionDto source)
    {
        public DialogGuiAction ToEntity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            Action = source.Action,
            Url = source.Url,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsDeleteDialogAction = source.IsDeleteDialogAction,
            PriorityId = source.Priority,
            HttpMethodId = source.HttpMethod ?? Domain.Http.HttpVerb.Values.GET,
            Title = source.Title.MergeInto<DialogGuiActionTitle>(null) ?? new(),
            Prompt = source.Prompt.MergeInto<DialogGuiActionPrompt>(null)
        };
    }

    extension(ApiActionDto source)
    {
        public DialogApiAction ToEntity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            Action = source.Action,
            AuthorizationAttribute = source.AuthorizationAttribute,
            Name = source.Name,
            Endpoints = source.Endpoints
                .Select(endpoint => endpoint.ToEntity())
                .ToList()
        };
    }

    extension(ApiActionEndpointDto source)
    {
        public DialogApiActionEndpoint ToEntity() => new()
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
    }

    extension(ActivityDto source)
    {
        public DialogActivity ToEntity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            CreatedAt = source.CreatedAt ?? default,
            ExtendedType = source.ExtendedType,
            TypeId = source.Type,
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToDialogActivityPerformedByActor(),
            Description = source.Description.MergeInto<DialogActivityDescription>(null)
        };
    }

    extension(TransmissionDto source)
    {
        public DialogTransmission ToEntity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            IdempotentKey = source.IdempotentKey,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            TypeId = source.Type,
            Sender = source.Sender.ToDialogTransmissionSenderActor(),
            Content = source.Content.ToDialogTransmissionContentEntities() ?? [],
            Attachments = source.Attachments
                .Select(attachment => attachment.ToEntity())
                .ToList(),
            NavigationalActions = source.NavigationalActions
                .Select(action => action.ToEntity())
                .ToList()
        };
    }

    extension(TransmissionAttachmentDto source)
    {
        public DialogTransmissionAttachment ToEntity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            DisplayName = source.DisplayName.MergeInto<AttachmentDisplayName>(null),
            Name = source.Name,
            Urls = source.Urls
                .Select(url => url.ToEntity())
                .ToList(),
            ExpiresAt = source.ExpiresAt
        };
    }

    extension(TransmissionAttachmentUrlDto source)
    {
        public AttachmentUrl ToEntity() => new()
        {
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerTypeId = source.ConsumerType
        };
    }

    extension(TransmissionNavigationalActionDto source)
    {
        public DialogTransmissionNavigationalAction ToEntity() => new()
        {
            Title = source.Title.MergeInto<DialogTransmissionNavigationalActionTitle>(null) ?? new(),
            Url = source.Url,
            ExpiresAt = source.ExpiresAt
        };
    }
}
