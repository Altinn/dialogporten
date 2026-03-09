using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using GetQueries = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using ActorDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors.ActorDto;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

internal static class DialogMapper
{
    extension(UpdateDialogDto source)
    {
        public void ApplyTo(DialogEntity destination)
        {
            destination.Progress = source.Progress;
            destination.ExtendedStatus = source.ExtendedStatus;
            destination.ExternalReference = source.ExternalReference;
            destination.DueAt = source.DueAt;
            destination.Process = source.Process;
            destination.PrecedingProcess = source.PrecedingProcess;
            destination.ExpiresAt = source.ExpiresAt;
            destination.IsApiOnly = source.IsApiOnly;
            destination.StatusId = source.Status.ToEntity();
            destination.Content = source.Content.ToDialogContentEntities(destination.Content) ?? [];
        }
    }

    extension(SearchTagDto source)
    {
        public DialogSearchTag ToEntity() => new()
        {
            Value = source.Value
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

        public void ApplyTo(DialogApiAction destination)
        {
            destination.Action = source.Action;
            destination.AuthorizationAttribute = source.AuthorizationAttribute;
            destination.Name = source.Name;
        }
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

        public void ApplyTo(DialogApiActionEndpoint destination)
        {
            destination.Version = source.Version;
            destination.Url = source.Url;
            destination.HttpMethodId = source.HttpMethod;
            destination.DocumentationUrl = source.DocumentationUrl;
            destination.RequestSchema = source.RequestSchema;
            destination.ResponseSchema = source.ResponseSchema;
            destination.Deprecated = source.Deprecated;
            destination.SunsetAt = source.SunsetAt;
        }
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
            HttpMethodId = source.HttpMethod ?? Domain.Http.HttpVerb.Values.GET,
            PriorityId = source.Priority,
            Title = source.Title.MergeInto<DialogGuiActionTitle>(null) ?? new(),
            Prompt = source.Prompt.MergeInto<DialogGuiActionPrompt>(null)
        };

        public void ApplyTo(DialogGuiAction destination)
        {
            destination.Action = source.Action;
            destination.Url = source.Url;
            destination.AuthorizationAttribute = source.AuthorizationAttribute;
            destination.IsDeleteDialogAction = source.IsDeleteDialogAction;
            destination.HttpMethodId = source.HttpMethod ?? Domain.Http.HttpVerb.Values.GET;
            destination.PriorityId = source.Priority;
            destination.Title = source.Title.MergeInto(destination.Title) ?? new DialogGuiActionTitle();
            destination.Prompt = source.Prompt.MergeInto(destination.Prompt);
        }
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

        public void ApplyTo(DialogAttachment destination)
        {
            destination.DisplayName = source.DisplayName.MergeInto(destination.DisplayName);
            destination.Name = source.Name;
            destination.ExpiresAt = source.ExpiresAt;
        }
    }

    extension(AttachmentUrlDto source)
    {
        public AttachmentUrl ToEntity() => new()
        {
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerTypeId = source.ConsumerType
        };

        public void ApplyTo(AttachmentUrl destination)
        {
            destination.Url = source.Url;
            destination.MediaType = source.MediaType;
            destination.ConsumerTypeId = source.ConsumerType;
        }
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

    extension(GetQueries.DialogDto source)
    {
        public UpdateDialogDto ToUpdateDialogDto() => new()
        {
            Progress = source.Progress,
            ExtendedStatus = source.ExtendedStatus,
            ExternalReference = source.ExternalReference,
            DueAt = source.DueAt,
            Process = source.Process,
            PrecedingProcess = source.PrecedingProcess,
            ExpiresAt = source.ExpiresAt,
            IsApiOnly = source.IsApiOnly,
            Status = (DialogStatusInput)source.Status,
            Content = source.Content?.ToUpdateDto(),
            SearchTags = source.SearchTags?
                .Select(tag => tag.ToUpdateDto())
                .ToList() ?? [],
            Attachments = source.Attachments
                .Select(attachment => attachment.ToUpdateDto())
                .ToList(),
            GuiActions = source.GuiActions
                .Select(action => action.ToUpdateDto())
                .ToList(),
            ApiActions = source.ApiActions
                .Select(action => action.ToUpdateDto())
                .ToList()
        };
    }

    extension(GetQueries.SearchTagDto source)
    {
        public SearchTagDto ToUpdateDto() => new()
        {
            Value = source.Value
        };
    }

    extension(GetQueries.DialogActivityDto source)
    {
        public ActivityDto ToUpdateDto() => new()
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            ExtendedType = source.ExtendedType,
            Type = source.Type,
            TransmissionId = source.TransmissionId,
            PerformedBy = Clone(source.PerformedBy),
            Description = Clone(source.Description)
        };
    }

    extension(GetQueries.DialogApiActionDto source)
    {
        public ApiActionDto ToUpdateDto() => new()
        {
            Id = source.Id,
            Action = source.Action,
            AuthorizationAttribute = source.AuthorizationAttribute,
            Name = source.Name,
            Endpoints = source.Endpoints
                .Select(endpoint => endpoint.ToUpdateDto())
                .ToList()
        };
    }

    extension(GetQueries.DialogApiActionEndpointDto source)
    {
        public ApiActionEndpointDto ToUpdateDto() => new()
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
    }

    extension(GetQueries.DialogGuiActionDto source)
    {
        public GuiActionDto ToUpdateDto() => new()
        {
            Id = source.Id,
            Action = source.Action,
            Url = source.Url,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsDeleteDialogAction = source.IsDeleteDialogAction,
            HttpMethod = source.HttpMethod,
            Priority = source.Priority,
            Title = Clone(source.Title),
            Prompt = source.Prompt is not null ? Clone(source.Prompt) : null
        };
    }

    extension(GetQueries.DialogAttachmentDto source)
    {
        public AttachmentDto ToUpdateDto() => new()
        {
            Id = source.Id,
            DisplayName = Clone(source.DisplayName),
            Name = source.Name,
            Urls = source.Urls
                .Select(url => url.ToUpdateDto())
                .ToList(),
            ExpiresAt = source.ExpiresAt
        };
    }

    extension(GetQueries.DialogAttachmentUrlDto source)
    {
        public AttachmentUrlDto ToUpdateDto() => new()
        {
            Id = source.Id,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerType = source.ConsumerType
        };
    }

    extension(GetQueries.ContentDto source)
    {
        public ContentDto ToUpdateDto() => new()
        {
            Title = Clone(source.Title),
            Summary = source.Summary is not null ? Clone(source.Summary) : null,
            SenderName = source.SenderName is not null ? Clone(source.SenderName) : null,
            AdditionalInfo = source.AdditionalInfo is not null ? Clone(source.AdditionalInfo) : null,
            ExtendedStatus = source.ExtendedStatus is not null ? Clone(source.ExtendedStatus) : null,
            MainContentReference = source.MainContentReference is not null ? Clone(source.MainContentReference) : null
        };
    }

    private static ActorDto Clone(ActorDto source) => new()
    {
        ActorType = source.ActorType,
        ActorName = source.ActorName,
        ActorId = source.ActorId
    };

    private static List<LocalizationDto> Clone(IEnumerable<LocalizationDto> source) => source
        .Select(localization => new LocalizationDto
        {
            LanguageCode = localization.LanguageCode,
            Value = localization.Value
        })
        .ToList();

    private static ContentValueDto Clone(ContentValueDto source) => new()
    {
        MediaType = source.MediaType,
        Value = Clone(source.Value)
    };
}
