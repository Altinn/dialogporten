using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchTransmissions;

internal static class TransmissionMapper
{
    extension(DialogTransmission source)
    {
        public TransmissionDto ToDto() => new()
        {
            Id = source.Id,
            IdempotentKey = source.IdempotentKey,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            DeletedAt = source.Dialog.DeletedAt,
            Type = source.TypeId,
            Sender = source.Sender.ToDto(),
            Content = source.Content.ToDialogTransmissionContentDto<ContentDto>()!,
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
        public AttachmentDto ToDto() => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName.ToDto() ?? [],
            Name = source.Name,
            Urls = source.Urls
                .Select(url => url.ToDto())
                .ToList(),
            ExpiresAt = source.ExpiresAt
        };
    }

    extension(AttachmentUrl source)
    {
        public AttachmentUrlDto ToDto() => new()
        {
            Id = source.Id,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerType = source.ConsumerTypeId
        };
    }

    extension(DialogTransmissionNavigationalAction source)
    {
        public NavigationalActionDto ToDto() => new()
        {
            Title = source.Title.ToDto() ?? [],
            Url = source.Url,
            ExpiresAt = source.ExpiresAt
        };
    }
}
