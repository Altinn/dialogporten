using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Get;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Search;

namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Mapping;

/// <summary>
/// Normalizes seen-log entries from the single-item (<c>Details</c>), list (<c>ListItem</c>) and search
/// (<c>SearchItem</c>) endpoint families into the base <see cref="DialogSeenLog"/> model. The <c>SeenBy</c>
/// actor is reused by reference. The <c>Details</c> and <c>SearchItem</c> families expose
/// <c>IsViaServiceOwner</c> as a non-nullable <see cref="bool"/>, which widens into the base nullable field.
/// </summary>
public static class SeenLogMappingExtensions
{
    /// <summary>Normalizes a <see cref="DialogSeenLogDetails"/> (single-item endpoint) into the base <see cref="DialogSeenLog"/>.</summary>
    public static DialogSeenLog ToDialogSeenLog(this DialogSeenLogDetails source) => new()
    {
        Id = source.Id,
        SeenAt = source.SeenAt,
        SeenBy = source.SeenBy,
        IsViaServiceOwner = source.IsViaServiceOwner,
        IsCurrentEndUser = source.IsCurrentEndUser,
    };

    /// <summary>Normalizes a <see cref="DialogSeenLogListItem"/> (list endpoint) into the base <see cref="DialogSeenLog"/>.</summary>
    public static DialogSeenLog ToDialogSeenLog(this DialogSeenLogListItem source) => new()
    {
        Id = source.Id,
        SeenAt = source.SeenAt,
        SeenBy = source.SeenBy,
        IsViaServiceOwner = source.IsViaServiceOwner,
        IsCurrentEndUser = source.IsCurrentEndUser,
    };

    /// <summary>Normalizes a <see cref="DialogSeenLogSearchItem"/> (search endpoint) into the base <see cref="DialogSeenLog"/>.</summary>
    public static DialogSeenLog ToDialogSeenLog(this DialogSeenLogSearchItem source) => new()
    {
        Id = source.Id,
        SeenAt = source.SeenAt,
        SeenBy = source.SeenBy,
        IsViaServiceOwner = source.IsViaServiceOwner,
        IsCurrentEndUser = source.IsCurrentEndUser,
    };
}
