using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;

internal static class Mappers
{
    // Attachments are merged separately by the handler.
    internal static void UpdateFrom(this DialogTransmission destination, UpdateTransmissionDto source)
    {
        if (source.CreatedAt.HasValue)
        {
            destination.CreatedAt = source.CreatedAt.Value;
        }

        destination.IdempotentKey = source.IdempotentKey;
        destination.AuthorizationAttribute = source.AuthorizationAttribute;
        destination.ExtendedType = source.ExtendedType;
        destination.ExternalReference = source.ExternalReference;
        destination.RelatedTransmissionId = source.RelatedTransmissionId;
        destination.TypeId = source.Type;
        destination.Sender = source.Sender.ToActor<DialogTransmissionSenderActor>();
        destination.Content = source.Content.ToDialogTransmissionContentList(destination.Content) ?? [];
        destination.NavigationalActions = source.NavigationalActions
            .Select(x => x.ToDialogTransmissionNavigationalAction())
            .ToList();
    }

    internal static DialogTransmissionAttachment ToDialogTransmissionAttachment(this TransmissionAttachmentDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Name = source.Name,
            ExpiresAt = source.ExpiresAt,
            DisplayName = source.DisplayName.ToLocalizationSet<AttachmentDisplayName>(),
            Urls = source.Urls.Select(x => x.ToAttachmentUrl()).ToList()
        };

    // Urls are replaced separately by the handler.
    internal static void UpdateFrom(this DialogTransmissionAttachment destination, TransmissionAttachmentDto source)
    {
        destination.Name = source.Name;
        destination.ExpiresAt = source.ExpiresAt;
        destination.DisplayName = source.DisplayName.ToLocalizationSet(destination.DisplayName);
    }

    internal static AttachmentUrl ToAttachmentUrl(this TransmissionAttachmentUrlDto source) =>
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
}
