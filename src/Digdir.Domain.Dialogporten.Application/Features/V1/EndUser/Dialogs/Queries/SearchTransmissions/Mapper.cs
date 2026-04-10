using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchTransmissions;

internal static class TransmissionMapExtensions
{
    extension(DialogTransmission source)
    {
        internal TransmissionDto ToDto() => new()
        {
            Id = source.Id,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            DeletedAt = source.Dialog.DeletedAt,
            Type = source.TypeId,
            Sender = source.Sender.ToDto(),
            Content = source.Content.ToList().ToTransmissionContentDto<ContentDto>()!,
            Attachments = source.Attachments.Select(a => a.ToDto()).ToList(),
            NavigationalActions = source.NavigationalActions.Select(n => n.ToDto()).ToList()
        };
    }
}

internal static class AttachmentMapExtensions
{
    extension(DialogTransmissionAttachment source)
    {
        internal AttachmentDto ToDto() => new()
        {
            Id = source.Id,
            DisplayName = source.DisplayName.ToDtoList() ?? [],
            Name = source.Name,
            Urls = source.Urls.Select(u => u.ToDto()).ToList(),
            ExpiresAt = source.ExpiresAt
        };
    }
}

internal static class AttachmentUrlMapExtensions
{
    extension(AttachmentUrl source)
    {
        internal AttachmentUrlDto ToDto() => new()
        {
            Id = source.Id,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerType = source.ConsumerTypeId
        };
    }
}

internal static class NavigationalActionMapExtensions
{
    extension(DialogTransmissionNavigationalAction source)
    {
        internal NavigationalActionDto ToDto() => new()
        {
            Title = source.Title.ToDtoList() ?? [],
            Url = source.Url,
            ExpiresAt = source.ExpiresAt
        };
    }
}
