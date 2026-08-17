using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Mapping;

/// <summary>
/// Normalizes the <see cref="DialogListItem"/> returned by the search/list endpoints into the base
/// <see cref="Dialog"/> model returned by <c>GetDialog</c>, so callers can treat every dialog payload
/// uniformly. Shared value types (<c>Actor</c>, <c>ContentValue</c>, <c>Localization</c>) are reused by
/// reference rather than copied.
/// <br/><br/>
/// This conversion is lossy by design: <see cref="DialogListItem"/> carries a reduced field set and omits
/// the collection-heavy parts of a dialog (<c>Transmissions</c>, <c>ApiActions</c>, <c>GuiActions</c>,
/// <c>Attachments</c>) which keep their model defaults (empty collections), as well as <c>DialogToken</c>,
/// <c>Revision</c> and <c>ExpiresAt</c> which are left null/default. The <c>GuiAttachmentCount</c> summary
/// count has no target and is dropped. The single <c>LatestActivity</c> is normalized into a one-element
/// <c>Activities</c> list, and the reduced <c>Content</c> summary is widened via
/// <see cref="ContentMappingExtensions"/>.
/// </summary>
public static class DialogMappingExtensions
{
    /// <summary>
    /// Maps a <see cref="DialogListItem"/> to a <see cref="Dialog"/>. See the type-level remarks for the
    /// fields that are dropped or defaulted because they have no source on a list item.
    /// </summary>
    public static Dialog ToDialog(this DialogListItem source) => new()
    {
        Id = source.Id,
        Org = source.Org,
        ServiceResource = source.ServiceResource,
        ServiceResourceType = source.ServiceResourceType,
        Party = source.Party,
        Progress = source.Progress,
        Process = source.Process,
        PrecedingProcess = source.PrecedingProcess,
        ExtendedStatus = source.ExtendedStatus,
        ExternalReference = source.ExternalReference,
        DueAt = source.DueAt,
        CreatedAt = source.CreatedAt,
        UpdatedAt = source.UpdatedAt,
        ContentUpdatedAt = source.ContentUpdatedAt,
        Status = source.Status,
        IsApiOnly = source.IsApiOnly,
        HasUnopenedContent = source.HasUnopenedContent,
        IsContentSeen = source.IsContentSeen,
        FromServiceOwnerTransmissionsCount = source.FromServiceOwnerTransmissionsCount,
        FromPartyTransmissionsCount = source.FromPartyTransmissionsCount,
        Content = source.Content?.ToContent()!,
        Activities = source.LatestActivity is null ? null : [source.LatestActivity.ToDialogActivity()],
        SeenSinceLastUpdate = source.SeenSinceLastUpdate?.Select(x => x.ToDialogSeenLog()).ToList(),
        SeenSinceLastContentUpdate = source.SeenSinceLastContentUpdate?.Select(x => x.ToDialogSeenLog()).ToList(),
        EndUserContext = source.EndUserContext.ToDialogEndUserContext(),
    };

    private static DialogEndUserContext ToDialogEndUserContext(this DialogEndUserContextListItem source) => new()
    {
        Revision = source.Revision,
        SystemLabels = source.SystemLabels,
    };
}
