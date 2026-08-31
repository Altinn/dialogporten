using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Update;

// The legacy authorizationAttribute/action members carried by these maps are [Obsolete] in favour of
// authorizationContext, but a mapping layer has to keep round-tripping them for as long as the server
// still returns and accepts them.
#pragma warning disable CS0618 // Type or member is obsolete

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

/// <summary>
/// Maps the dialog transmission hierarchies (transmission + content + attachment + URL + navigational
/// action). Get-only server fields such as <c>IsAuthorized</c>/<c>IsOpened</c> have no target on the input
/// models and are dropped. The <c>Sender</c> actor, localized titles and content values are shared types and
/// are reused by reference. Note that transmission attachment URLs carry no Id on the input models, so the
/// server-assigned Id is dropped when mapping from Get.
/// </summary>
internal static class TransmissionMappingExtensions
{
    // Transmissions

    internal static CreateDialogTransmission ToCreateDialogTransmission(this DialogTransmission source) => new()
    {
        Id = source.Id,
        IdempotentKey = source.IdempotentKey,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type,
        Sender = source.Sender,
        Content = source.Content.ToCreateDialogTransmissionContent(),
        Attachments = source.Attachments?.Select(x => x.ToCreateDialogTransmissionAttachment()).ToList() ?? [],
        NavigationalActions = source.NavigationalActions?.Select(x => x.ToCreateDialogTransmissionNavigationalAction()).ToList() ?? [],
    };

    internal static UpdateDialogTransmission ToUpdateDialogTransmission(this DialogTransmission source) => new()
    {
        Id = source.Id,
        IdempotentKey = source.IdempotentKey,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type,
        Sender = source.Sender,
        Content = source.Content.ToUpdateDialogTransmissionContent(),
        Attachments = source.Attachments?.Select(x => x.ToUpdateDialogTransmissionAttachment()).ToList() ?? [],
        NavigationalActions = source.NavigationalActions?.Select(x => x.ToUpdateDialogTransmissionNavigationalAction()).ToList() ?? [],
    };

    internal static UpdateDialogTransmission ToUpdateDialogTransmission(this CreateDialogTransmission source) => new()
    {
        Id = source.Id,
        IdempotentKey = source.IdempotentKey,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext,
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type,
        Sender = source.Sender,
        Content = source.Content?.ToUpdateDialogTransmissionContent(),
        Attachments = source.Attachments?.Select(x => x.ToUpdateDialogTransmissionAttachment()).ToList() ?? [],
        NavigationalActions = source.NavigationalActions?.Select(x => x.ToUpdateDialogTransmissionNavigationalAction()).ToList() ?? [],
    };

    internal static CreateDialogTransmission ToCreateDialogTransmission(this UpdateDialogTransmission source) => new()
    {
        Id = source.Id,
        IdempotentKey = source.IdempotentKey,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext,
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type,
        Sender = source.Sender,
        Content = source.Content?.ToCreateDialogTransmissionContent(),
        Attachments = source.Attachments?.Select(x => x.ToCreateDialogTransmissionAttachment()).ToList() ?? [],
        NavigationalActions = source.NavigationalActions?.Select(x => x.ToCreateDialogTransmissionNavigationalAction()).ToList() ?? [],
    };

    // Create/Update -> Get. Get-only server fields (IsAuthorized, IsOpened) have no source and keep their defaults.
    // Content is required on the Get model but optional on the input models; a missing content is carried over as
    // null (null-forgiven) rather than fabricated.

    internal static DialogTransmission ToDialogTransmission(this CreateDialogTransmission source) => new()
    {
        Id = source.Id ?? default,
        IdempotentKey = source.IdempotentKey,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type,
        Sender = source.Sender,
        Content = source.Content?.ToDialogTransmissionContent()!,
        Attachments = source.Attachments?.Select(x => x.ToDialogTransmissionAttachment()).ToList() ?? [],
        NavigationalActions = source.NavigationalActions?.Select(x => x.ToDialogTransmissionNavigationalAction()).ToList() ?? [],
    };

    internal static DialogTransmission ToDialogTransmission(this UpdateDialogTransmission source) => new()
    {
        Id = source.Id ?? default,
        IdempotentKey = source.IdempotentKey,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type,
        Sender = source.Sender,
        Content = source.Content?.ToDialogTransmissionContent()!,
        Attachments = source.Attachments?.Select(x => x.ToDialogTransmissionAttachment()).ToList() ?? [],
        NavigationalActions = source.NavigationalActions?.Select(x => x.ToDialogTransmissionNavigationalAction()).ToList() ?? [],
    };

    // Transmission content

    private static CreateDialogTransmissionContent ToCreateDialogTransmissionContent(this DialogTransmissionContent source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        ContentReference = source.ContentReference,
    };

    private static UpdateDialogTransmissionContent ToUpdateDialogTransmissionContent(this DialogTransmissionContent source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        ContentReference = source.ContentReference,
    };

    private static UpdateDialogTransmissionContent ToUpdateDialogTransmissionContent(this CreateDialogTransmissionContent source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        ContentReference = source.ContentReference,
    };

    private static CreateDialogTransmissionContent ToCreateDialogTransmissionContent(this UpdateDialogTransmissionContent source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        ContentReference = source.ContentReference,
    };

    private static DialogTransmissionContent ToDialogTransmissionContent(this CreateDialogTransmissionContent source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        ContentReference = source.ContentReference,
    };

    private static DialogTransmissionContent ToDialogTransmissionContent(this UpdateDialogTransmissionContent source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        ContentReference = source.ContentReference,
    };

    // Transmission attachments

    private static CreateDialogTransmissionAttachment ToCreateDialogTransmissionAttachment(this DialogTransmissionAttachment source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToCreateDialogTransmissionAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
    };

    private static UpdateDialogTransmissionAttachment ToUpdateDialogTransmissionAttachment(this DialogTransmissionAttachment source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToUpdateDialogTransmissionAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
    };

    private static UpdateDialogTransmissionAttachment ToUpdateDialogTransmissionAttachment(this CreateDialogTransmissionAttachment source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToUpdateDialogTransmissionAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext,
    };

    private static CreateDialogTransmissionAttachment ToCreateDialogTransmissionAttachment(this UpdateDialogTransmissionAttachment source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToCreateDialogTransmissionAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext,
    };

    private static DialogTransmissionAttachment ToDialogTransmissionAttachment(this CreateDialogTransmissionAttachment source) => new()
    {
        Id = source.Id ?? default,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToDialogTransmissionAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
    };

    private static DialogTransmissionAttachment ToDialogTransmissionAttachment(this UpdateDialogTransmissionAttachment source) => new()
    {
        Id = source.Id ?? default,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToDialogTransmissionAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
    };

    // Transmission attachment URLs (no Id on the input models)

    private static CreateDialogTransmissionAttachmentUrl ToCreateDialogTransmissionAttachmentUrl(this DialogTransmissionAttachmentUrl source) => new()
    {
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    private static UpdateDialogTransmissionAttachmentUrl ToUpdateDialogTransmissionAttachmentUrl(this DialogTransmissionAttachmentUrl source) => new()
    {
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    private static UpdateDialogTransmissionAttachmentUrl ToUpdateDialogTransmissionAttachmentUrl(this CreateDialogTransmissionAttachmentUrl source) => new()
    {
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    private static CreateDialogTransmissionAttachmentUrl ToCreateDialogTransmissionAttachmentUrl(this UpdateDialogTransmissionAttachmentUrl source) => new()
    {
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    // The input models carry no Id for transmission attachment URLs, so the Get Id is left as its default.

    private static DialogTransmissionAttachmentUrl ToDialogTransmissionAttachmentUrl(this CreateDialogTransmissionAttachmentUrl source) => new()
    {
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    private static DialogTransmissionAttachmentUrl ToDialogTransmissionAttachmentUrl(this UpdateDialogTransmissionAttachmentUrl source) => new()
    {
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    // Transmission navigational actions

    private static CreateDialogTransmissionNavigationalAction ToCreateDialogTransmissionNavigationalAction(this DialogTransmissionNavigationalAction source) => new()
    {
        Title = source.Title,
        Url = source.Url,
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
    };

    private static UpdateDialogTransmissionNavigationalAction ToUpdateDialogTransmissionNavigationalAction(this DialogTransmissionNavigationalAction source) => new()
    {
        Title = source.Title,
        Url = source.Url,
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
    };

    private static UpdateDialogTransmissionNavigationalAction ToUpdateDialogTransmissionNavigationalAction(this CreateDialogTransmissionNavigationalAction source) => new()
    {
        Title = source.Title,
        Url = source.Url,
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext,
    };

    private static CreateDialogTransmissionNavigationalAction ToCreateDialogTransmissionNavigationalAction(this UpdateDialogTransmissionNavigationalAction source) => new()
    {
        Title = source.Title,
        Url = source.Url,
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext,
    };

    private static DialogTransmissionNavigationalAction ToDialogTransmissionNavigationalAction(this CreateDialogTransmissionNavigationalAction source) => new()
    {
        Title = source.Title,
        Url = source.Url,
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
    };

    private static DialogTransmissionNavigationalAction ToDialogTransmissionNavigationalAction(this UpdateDialogTransmissionNavigationalAction source) => new()
    {
        Title = source.Title,
        Url = source.Url,
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
    };
}

#pragma warning restore CS0618
