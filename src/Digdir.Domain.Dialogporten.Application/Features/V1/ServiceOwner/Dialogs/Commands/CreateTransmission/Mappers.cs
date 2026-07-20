using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;

internal static class Mappers
{
    internal static DialogTransmission ToDialogTransmission(this CreateTransmissionDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            IdempotentKey = source.IdempotentKey,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            TypeId = source.Type,
            Sender = source.Sender.ToActor<DialogTransmissionSenderActor>(),
            Content = source.Content.ToDialogTransmissionContentList() ?? [],
            Attachments = source.Attachments.Select(x => x.ToDialogTransmissionAttachment()).ToList(),
            NavigationalActions = source.NavigationalActions.Select(x => x.ToDialogTransmissionNavigationalAction()).ToList()
        };

    private static DialogTransmissionAttachment ToDialogTransmissionAttachment(this TransmissionAttachmentDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Name = source.Name,
            ExpiresAt = source.ExpiresAt,
            DisplayName = source.DisplayName.ToLocalizationSet<AttachmentDisplayName>(),
            Urls = source.Urls.Select(x => x.ToAttachmentUrl()).ToList()
        };

    private static AttachmentUrl ToAttachmentUrl(this TransmissionAttachmentUrlDto source) =>
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
