using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

internal static class GraphQlMapper
{
    extension(DialogDto source)
    {
        public Dialog ToGraphQl() => new()
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
            DialogToken = source.DialogToken,
            Status = source.Status.ToGraphQl(),
            HasUnopenedContent = source.HasUnopenedContent,
            FromServiceOwnerTransmissionsCount = source.FromServiceOwnerTransmissionsCount,
            FromPartyTransmissionsCount = source.FromPartyTransmissionsCount,
            IsApiOnly = source.IsApiOnly,
            Content = source.Content.ToGraphQl(),
            Attachments = source.Attachments.Select(attachment => attachment.ToGraphQl()).ToList(),
            GuiActions = source.GuiActions.Select(action => action.ToGraphQl()).ToList(),
            ApiActions = source.ApiActions.Select(action => action.ToGraphQl()).ToList(),
            Activities = source.Activities.Select(activity => activity.ToGraphQl()).ToList(),
            SeenSinceLastUpdate = source.SeenSinceLastUpdate.Select(log => log.ToGraphQl()).ToList(),
            SeenSinceLastContentUpdate = source.SeenSinceLastContentUpdate.Select(log => log.ToGraphQl()).ToList(),
            Transmissions = source.Transmissions.Select(transmission => transmission.ToGraphQl()).ToList(),
            EndUserContext = source.EndUserContext.ToGraphQl()
        };
    }

    extension(DialogEndUserContextDto source)
    {
        public EndUserContext ToGraphQl() => new()
        {
            Revision = source.Revision,
            SystemLabels = source.SystemLabels.Select(label => (SystemLabel)label).ToList()
        };
    }

    extension(DialogAttachmentDto source)
    {
        public Attachment ToGraphQl() => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName?.Select(localization => localization.ToGraphQl()).ToList() ?? [],
            Name = source.Name,
            Urls = source.Urls.Select(url => url.ToGraphQl()).ToList(),
            ExpiresAt = source.ExpiresAt
        };
    }

    extension(DialogAttachmentUrlDto source)
    {
        public AttachmentUrl ToGraphQl() => new()
        {
            Id = source.Id,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerType = (AttachmentUrlConsumer)source.ConsumerType
        };
    }

    extension(DialogGuiActionDto source)
    {
        public GuiAction ToGraphQl() => new()
        {
            Id = source.Id,
            Action = source.Action,
            Url = source.Url,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsAuthorized = source.IsAuthorized,
            IsDeleteDialogAction = source.IsDeleteDialogAction,
            Priority = (GuiActionPriority)source.Priority,
            HttpMethod = (HttpVerb)source.HttpMethod,
            Title = source.Title.Select(localization => localization.ToGraphQl()).ToList(),
            Prompt = source.Prompt?.Select(localization => localization.ToGraphQl()).ToList() ?? []
        };
    }

    extension(DialogApiActionDto source)
    {
        public ApiAction ToGraphQl() => new()
        {
            Id = source.Id,
            Action = source.Action,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsAuthorized = source.IsAuthorized,
            Name = source.Name,
            Endpoints = source.Endpoints.Select(endpoint => endpoint.ToGraphQl()).ToList()
        };
    }

    extension(DialogApiActionEndpointDto source)
    {
        public ApiActionEndpoint ToGraphQl() => new()
        {
            Id = source.Id,
            Version = source.Version,
            Url = source.Url,
            HttpMethod = (HttpVerb)source.HttpMethod,
            DocumentationUrl = source.DocumentationUrl,
            RequestSchema = source.RequestSchema,
            ResponseSchema = source.ResponseSchema,
            Deprecated = source.Deprecated,
            SunsetAt = source.SunsetAt
        };
    }

    extension(ContentDto source)
    {
        public Content ToGraphQl() => new()
        {
            Title = source.Title.ToGraphQl(),
            Summary = source.Summary?.ToGraphQl(),
            SenderName = source.SenderName?.ToGraphQl(),
            AdditionalInfo = source.AdditionalInfo?.ToGraphQl(),
            ExtendedStatus = source.ExtendedStatus?.ToGraphQl(),
            MainContentReference = source.MainContentReference?.ToGraphQl()
        };
    }

    extension(DialogTransmissionDto source)
    {
        public Transmission ToGraphQl() => new()
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsAuthorized = source.IsAuthorized,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            Type = (TransmissionType)source.Type,
            Sender = source.Sender.ToGraphQl(),
            IsOpened = source.IsOpened,
            Content = source.Content.ToGraphQl(),
            Attachments = source.Attachments.Select(attachment => attachment.ToGraphQl()).ToList(),
            NavigationalActions = source.NavigationalActions.Select(action => action.ToGraphQl()).ToList()
        };
    }

    extension(DialogTransmissionAttachmentDto source)
    {
        public Attachment ToGraphQl() => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName?.Select(localization => localization.ToGraphQl()).ToList() ?? [],
            Name = source.Name,
            Urls = source.Urls.Select(url => url.ToGraphQl()).ToList(),
            ExpiresAt = source.ExpiresAt
        };
    }

    extension(DialogTransmissionAttachmentUrlDto source)
    {
        public AttachmentUrl ToGraphQl() => new()
        {
            Id = source.Id,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerType = (AttachmentUrlConsumer)source.ConsumerType
        };
    }

    extension(DialogTransmissionContentDto source)
    {
        public TransmissionContent ToGraphQl() => new()
        {
            Title = source.Title.ToGraphQl(),
            Summary = source.Summary?.ToGraphQl(),
            ContentReference = source.ContentReference?.ToGraphQl()
        };
    }

    extension(DialogTransmissionNavigationalActionDto source)
    {
        public TransmissionNavigationalAction ToGraphQl() => new()
        {
            Title = source.Title.Select(localization => localization.ToGraphQl()).ToList(),
            Url = source.Url,
            ExpiresAt = source.ExpiresAt
        };
    }
}
