using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;

internal static class TransmissionMapper
{
    extension(CreateTransmissionDto source)
    {
        public DialogTransmission ToEntity() => new()
        {
            Id = source.Id ?? Guid.Empty,
            IdempotentKey = source.IdempotentKey,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
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
