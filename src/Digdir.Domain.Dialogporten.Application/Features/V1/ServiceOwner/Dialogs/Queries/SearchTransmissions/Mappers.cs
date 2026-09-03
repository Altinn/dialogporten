#pragma warning disable CS0618 // Obsolete legacy authorization fields are mapped for backwards compatibility
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchTransmissions;

internal static class TransmissionMapExtensions
{
    extension(DialogTransmission source)
    {
        internal TransmissionDto ToDto() => new()
        {
            Id = source.Id,
            IdempotentKey = source.IdempotentKey,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.EffectiveLegacyAuthorizationAttribute,
            AuthorizationContext = source.AuthorizationContext.ToDto(),
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
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
            AuthorizationContext = source.AuthorizationContext.ToDto(),
            DisplayName = source.DisplayName.ToDtoList()!,
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
            AuthorizationContext = source.AuthorizationContext.ToDto(),
            Title = source.Title.ToDtoList()!,
            Url = source.Url,
            ExpiresAt = source.ExpiresAt
        };
    }
}

internal static class AuthorizationContextMapExtensions
{
    extension(Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts.AuthorizationContext? source)
    {
        internal AuthorizationContextDto? ToDto() => source is null
            ? null
            : new AuthorizationContextDto
            {
                ServiceResource = source.ServiceResource,
                AdditionalResourceAttribute = source.AdditionalResourceAttribute,
                Parties = [.. source.Parties],
                IncludeDialogParty = source.IncludeDialogParty,
                Action = source.Action,
                TokenRef = source.TokenReference,
                UnauthorizedPresentation = source.UnauthorizedPresentation
            };
    }
}
