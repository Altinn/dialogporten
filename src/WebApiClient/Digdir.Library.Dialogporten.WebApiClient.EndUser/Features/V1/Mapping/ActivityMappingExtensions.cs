using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Mapping;

/// <summary>
/// Normalizes activity items from the single-item (<c>Details</c>), list (<c>ListItem</c>) and search
/// (<c>SearchItem</c>) endpoint families into the base <see cref="DialogActivity"/> model. The
/// <c>PerformedBy</c> actor and localized description are reused by reference.
/// <br/><br/>
/// <see cref="DialogActivitySearchItem.CreatedAt"/> is non-nullable and widens into the base nullable
/// field. The search item carries no <c>PerformedBy</c>, so the base actor is left null when mapping from
/// a search item.
/// </summary>
public static class ActivityMappingExtensions
{
    /// <summary>Normalizes a <see cref="DialogActivityDetails"/> (single-item endpoint) into the base <see cref="DialogActivity"/>.</summary>
    public static DialogActivity ToDialogActivity(this DialogActivityDetails source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description,
    };

    /// <summary>Normalizes a <see cref="DialogActivityListItem"/> (e.g. a list item's latest activity) into the base <see cref="DialogActivity"/>.</summary>
    public static DialogActivity ToDialogActivity(this DialogActivityListItem source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = source.PerformedBy,
        Description = source.Description,
    };

    /// <summary>
    /// Normalizes a <see cref="DialogActivitySearchItem"/> (search endpoint) into the base
    /// <see cref="DialogActivity"/>. The search item has no <c>PerformedBy</c>, so the base actor is left null.
    /// </summary>
    public static DialogActivity ToDialogActivity(this DialogActivitySearchItem source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        Description = source.Description,
    };
}
