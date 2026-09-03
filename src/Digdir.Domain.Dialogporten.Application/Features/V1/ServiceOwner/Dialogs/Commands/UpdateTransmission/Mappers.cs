#pragma warning disable CS0618 // Obsolete legacy authorization fields are mapped for backwards compatibility
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;

internal static class Mappers
{
    // Attachments and navigational actions are merged separately by the handler.
    internal static void UpdateFrom(this DialogTransmission destination, UpdateTransmissionDto source)
    {
        if (source.CreatedAt.HasValue)
        {
            destination.CreatedAt = source.CreatedAt.Value;
        }

        destination.IdempotentKey = source.IdempotentKey;
        destination.AuthorizationAttribute = source.AuthorizationAttribute.ToStoredTransmissionAttribute(source.AuthorizationContext);
        destination.ExtendedType = source.ExtendedType;
        destination.ExternalReference = source.ExternalReference;
        destination.RelatedTransmissionId = source.RelatedTransmissionId;
        destination.TypeId = source.Type;
        destination.Sender = source.Sender.ToActor<DialogTransmissionSenderActor>();
        destination.AuthorizationContext = source.AuthorizationContext
            .ToAuthorizationContext(destination.AuthorizationContext);
        destination.Content = source.Content.ToDialogTransmissionContentList(destination.Content) ?? [];
    }

    internal static DialogTransmissionAttachment ToDialogTransmissionAttachment(this TransmissionAttachmentDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Name = source.Name,
            ExpiresAt = source.ExpiresAt,
            DisplayName = source.DisplayName.ToLocalizationSet<AttachmentDisplayName>(),
            Urls = source.Urls.Select(x => x.ToAttachmentUrl()).ToList(),
            AuthorizationContext = source.AuthorizationContext.ToAuthorizationContext<AttachmentAuthorizationContext>()
        };

    // Urls are merged separately by the handler.
    internal static void UpdateFrom(this DialogTransmissionAttachment destination, TransmissionAttachmentDto source)
    {
        destination.Name = source.Name;
        destination.ExpiresAt = source.ExpiresAt;
        destination.DisplayName = source.DisplayName.ToLocalizationSet(destination.DisplayName);
        destination.AuthorizationContext = source.AuthorizationContext
            .ToAuthorizationContext(destination.AuthorizationContext);
    }

    internal static AttachmentUrl ToAttachmentUrl(this TransmissionAttachmentUrlDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerTypeId = source.ConsumerType
        };

    // Only reached for urls matched on Id, so source.Id always equals destination.Id and is never null.
    internal static void UpdateFrom(this AttachmentUrl destination, TransmissionAttachmentUrlDto source)
    {
        destination.Url = source.Url;
        destination.MediaType = source.MediaType;
        destination.ConsumerTypeId = source.ConsumerType;
    }

    internal static DialogTransmissionNavigationalAction ToDialogTransmissionNavigationalAction(
        this TransmissionNavigationalActionDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Url = source.Url,
            ExpiresAt = source.ExpiresAt,
            Title = source.Title.ToLocalizationSet<DialogTransmissionNavigationalActionTitle>()!,
            AuthorizationContext = source.AuthorizationContext.ToAuthorizationContext<DialogTransmissionNavigationalActionAuthorizationContext>()
        };

    // Only reached for navigational actions matched on Id. Updating in place keeps the id stable, which
    // matters because it is emitted in the dialog token's authorized-entities claim.
    internal static void UpdateFrom(this DialogTransmissionNavigationalAction destination, TransmissionNavigationalActionDto source)
    {
        destination.Url = source.Url;
        destination.ExpiresAt = source.ExpiresAt;
        destination.Title = source.Title.ToLocalizationSet(destination.Title)!;
        destination.AuthorizationContext = source.AuthorizationContext
            .ToAuthorizationContext(destination.AuthorizationContext);
    }
}
