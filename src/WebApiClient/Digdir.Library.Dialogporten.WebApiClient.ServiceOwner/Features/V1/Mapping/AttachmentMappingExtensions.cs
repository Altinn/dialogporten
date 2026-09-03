using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Create;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Update;

// The authorizationContext members these maps carry are flagged experimental for consumers; the SDK's
// own mapping layer is part of the feature, so it opts in rather than warning about itself.
#pragma warning disable DPEXP001

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.Features.V1.Mapping;

/// <summary>
/// Maps the dialog-level attachment hierarchies (attachment + URL). The localized display name is a
/// shared <c>Localization</c> collection and is reused by reference.
/// </summary>
internal static class AttachmentMappingExtensions
{
    internal static CreateDialogAttachment ToCreateDialogAttachment(this DialogAttachment source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToCreateDialogAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
    };

    internal static UpdateDialogAttachment ToUpdateDialogAttachment(this DialogAttachment source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToUpdateDialogAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContextInput(),
    };

    internal static UpdateDialogAttachment ToUpdateDialogAttachment(this CreateDialogAttachment source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToUpdateDialogAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext,
    };

    internal static CreateDialogAttachment ToCreateDialogAttachment(this UpdateDialogAttachment source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToCreateDialogAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext,
    };

    private static CreateDialogAttachmentUrl ToCreateDialogAttachmentUrl(this DialogAttachmentUrl source) => new()
    {
        Id = source.Id,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    private static UpdateDialogAttachmentUrl ToUpdateDialogAttachmentUrl(this DialogAttachmentUrl source) => new()
    {
        Id = source.Id,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    private static UpdateDialogAttachmentUrl ToUpdateDialogAttachmentUrl(this CreateDialogAttachmentUrl source) => new()
    {
        Id = source.Id,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    private static CreateDialogAttachmentUrl ToCreateDialogAttachmentUrl(this UpdateDialogAttachmentUrl source) => new()
    {
        Id = source.Id,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    // Create/Update -> Get. Get requires a non-null Id; the input Id is optional (server-generated when omitted)
    // and is defaulted to Guid.Empty when absent.

    internal static DialogAttachment ToDialogAttachment(this CreateDialogAttachment source) => new()
    {
        Id = source.Id ?? default,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToDialogAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
    };

    internal static DialogAttachment ToDialogAttachment(this UpdateDialogAttachment source) => new()
    {
        Id = source.Id ?? default,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToDialogAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        AuthorizationContext = source.AuthorizationContext?.ToAuthorizationContext(),
    };

    private static DialogAttachmentUrl ToDialogAttachmentUrl(this CreateDialogAttachmentUrl source) => new()
    {
        Id = source.Id ?? default,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    private static DialogAttachmentUrl ToDialogAttachmentUrl(this UpdateDialogAttachmentUrl source) => new()
    {
        Id = source.Id ?? default,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };
}

#pragma warning restore DPEXP001
