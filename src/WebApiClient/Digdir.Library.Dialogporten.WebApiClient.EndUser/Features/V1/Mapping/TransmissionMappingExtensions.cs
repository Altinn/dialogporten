using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

// The end user contracts mark authorizationAttribute [Obsolete] in favour of the service owner API's
// authorizationContext, but this layer has to keep carrying it for as long as the server returns it.
#pragma warning disable CS0618 // Type or member is obsolete

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Mapping;

/// <summary>
/// Normalizes the transmission sub-tree (transmission + content + attachment + URL + navigational action)
/// from the single-item (<c>Details</c>) and list (<c>SearchItem</c>) endpoint families into the base
/// <see cref="DialogTransmission"/> model embedded in <see cref="Dialog"/>. The shared <c>Sender</c> actor,
/// localized titles and <c>ContentValue</c>s are reused by reference rather than copied.
/// <br/><br/>
/// These conversions are lossy by design: the source-only <c>DeletedAt</c> field has no target on the base
/// model and is dropped, and the base <c>IsOpened</c> flag has no source on either input family, so it
/// defaults to <see langword="false"/>.
/// </summary>
public static class TransmissionMappingExtensions
{
    // Transmissions

    /// <summary>
    /// Normalizes a <see cref="DialogTransmissionDetails"/> (single-item endpoint) into the base
    /// <see cref="DialogTransmission"/>. The source-only <c>DeletedAt</c> is dropped and the base
    /// <c>IsOpened</c> flag has no source, so it defaults to <see langword="false"/>.
    /// </summary>
    public static DialogTransmission ToDialogTransmission(this DialogTransmissionDetails source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        IsAuthorized = source.IsAuthorized,
        ContextToken = source.ContextToken,
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type,
        Sender = source.Sender,
        Content = source.Content.ToDialogTransmissionContent(),
        Attachments = source.Attachments?.Select(x => x.ToDialogTransmissionAttachment()).ToList() ?? [],
        NavigationalActions = source.NavigationalActions?.Select(x => x.ToDialogTransmissionNavigationalAction()).ToList() ?? [],
        ExcludedAttachments = source.ExcludedAttachments,
        ExcludedNavigationalActions = source.ExcludedNavigationalActions,
    };

    /// <summary>
    /// Normalizes a <see cref="DialogTransmissionSearchItem"/> (list endpoint) into the base
    /// <see cref="DialogTransmission"/>. The source-only <c>DeletedAt</c> is dropped and the base
    /// <c>IsOpened</c> flag has no source, so it defaults to <see langword="false"/>.
    /// </summary>
    public static DialogTransmission ToDialogTransmission(this DialogTransmissionSearchItem source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        IsAuthorized = source.IsAuthorized,
        ContextToken = source.ContextToken,
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type,
        Sender = source.Sender,
        Content = source.Content.ToDialogTransmissionContent(),
        Attachments = source.Attachments?.Select(x => x.ToDialogTransmissionAttachment()).ToList() ?? [],
        NavigationalActions = source.NavigationalActions?.Select(x => x.ToDialogTransmissionNavigationalAction()).ToList() ?? [],
        ExcludedAttachments = source.ExcludedAttachments,
        ExcludedNavigationalActions = source.ExcludedNavigationalActions,
    };

    // Transmission content

    private static DialogTransmissionContent ToDialogTransmissionContent(this DialogTransmissionContentDetails source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        ContentReference = source.ContentReference,
    };

    private static DialogTransmissionContent ToDialogTransmissionContent(this DialogTransmissionSearchContent source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        ContentReference = source.ContentReference,
    };

    // Transmission attachments

    private static DialogTransmissionAttachment ToDialogTransmissionAttachment(this DialogTransmissionAttachmentDetails source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToDialogTransmissionAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        IsAuthorized = source.IsAuthorized,
        ContextToken = source.ContextToken,
    };

    private static DialogTransmissionAttachment ToDialogTransmissionAttachment(this DialogTransmissionSearchAttachment source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls?.Select(x => x.ToDialogTransmissionAttachmentUrl()).ToList() ?? [],
        ExpiresAt = source.ExpiresAt,
        IsAuthorized = source.IsAuthorized,
        ContextToken = source.ContextToken,
    };

    // Transmission attachment URLs

    private static DialogTransmissionAttachmentUrl ToDialogTransmissionAttachmentUrl(this DialogTransmissionAttachmentUrlDetails source) => new()
    {
        Id = source.Id,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    private static DialogTransmissionAttachmentUrl ToDialogTransmissionAttachmentUrl(this DialogTransmissionSearchAttachmentUrl source) => new()
    {
        Id = source.Id,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType,
    };

    // Transmission navigational actions

    private static DialogTransmissionNavigationalAction ToDialogTransmissionNavigationalAction(this DialogTransmissionNavigationalActionDetails source) => new()
    {
        Title = source.Title,
        Url = source.Url,
        ExpiresAt = source.ExpiresAt,
        IsAuthorized = source.IsAuthorized,
        ContextToken = source.ContextToken,
    };

    private static DialogTransmissionNavigationalAction ToDialogTransmissionNavigationalAction(this DialogTransmissionSearchNavigationalAction source) => new()
    {
        Title = source.Title,
        Url = source.Url,
        ExpiresAt = source.ExpiresAt,
        IsAuthorized = source.IsAuthorized,
        ContextToken = source.ContextToken,
    };
}

#pragma warning restore CS0618
