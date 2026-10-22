using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;

namespace Digdir.Domain.Dialogporten.Application.Externals;

/// <summary>
/// A relation as seen from a given dialog: the other dialog and the type, inverted when the dialog is stored on the
/// right-hand side of the canonical row.
/// </summary>
public sealed record DialogRelationView(Guid RelatedDialogId, DialogRelationType.Values Type);

public sealed record EndUserDialogRelationsQuery(
    Guid DialogId,
    DialogSearchAuthorizationResult AuthorizedResources,
    DateTimeOffset Now,
    DialogRelationType.Values? Type,
    IReadOnlyCollection<string> Parties,
    IReadOnlyCollection<string> ServiceResources,
    IReadOnlyCollection<DialogStatus.Values> Statuses,
    bool ExcludeApiOnly,
    Guid? After,
    int Limit);

/// <summary>
/// A related dialog visible to the end user, with the relation type as seen from the anchor dialog.
/// </summary>
public sealed class EndUserRelatedDialog
{
    public Guid RelatedDialogId { get; init; }
    public int TypeId { get; init; }
    public string Org { get; init; } = null!;
    public string ServiceResource { get; init; } = null!;
    public string ServiceResourceType { get; init; } = null!;
    public string Party { get; init; } = null!;
    public int StatusId { get; init; }
    public string? ExtendedStatus { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset ContentUpdatedAt { get; init; }
    public DateTimeOffset? DueAt { get; init; }
    public bool IsApiOnly { get; init; }
    public bool HasRelations { get; init; }
}

public interface IDialogRelationRepository
{
    /// <summary>
    /// Reads the relations of a dialog through both index ranges (<c>UNION ALL</c>), ordered by the other dialog's id
    /// (PostgreSQL uuid order), with keyset pagination on that id.
    /// </summary>
    Task<List<DialogRelationView>> GetRelations(
        Guid dialogId,
        DialogRelationType.Values? type,
        Guid? after,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// The related dialogs the end user may see, by the same rules as the end-user search: authorized through the
    /// user's authorized parties (or an instance delegation), visible, not expired and not deleted. Ordered and
    /// paginated by the related dialog's id. Unauthorized relations are left out silently.
    /// </summary>
    Task<List<EndUserRelatedDialog>> SearchAsEndUser(EndUserDialogRelationsQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Counts the relations of a dialog, but stops counting at <paramref name="cap"/>.
    /// </summary>
    Task<int> CountRelations(Guid dialogId, int cap, CancellationToken cancellationToken);

    /// <summary>
    /// The ancestors of a dialog, nearest (the parent) first, walking at most <paramref name="maxSteps"/> parent
    /// relations.
    /// </summary>
    Task<List<Guid>> GetAncestors(Guid dialogId, int maxSteps, CancellationToken cancellationToken);

    /// <summary>
    /// Whether the dialog has descendants <paramref name="generations"/> parent/child relations below it.
    /// </summary>
    Task<bool> HasDescendantsAt(Guid dialogId, int generations, CancellationToken cancellationToken);

    /// <summary>
    /// Locks the dialog rows (<c>SELECT ... FOR UPDATE</c>, in id order to avoid deadlocks) until the current
    /// transaction ends. Returns the ids of the dialogs that exist and are not deleted.
    /// </summary>
    Task<HashSet<Guid>> LockDialogs(IEnumerable<Guid> dialogIds, CancellationToken cancellationToken);

    /// <summary>
    /// Sets <c>HasRelations</c> directly in the database, outside change tracking, so the dialog is not bumped and
    /// no events are emitted for it.
    /// </summary>
    Task SetHasRelations(Guid dialogId, bool hasRelations, CancellationToken cancellationToken);

    /// <summary>
    /// Clears <c>HasRelations</c> (without a bump) on the given dialogs that have no relations left, ignoring
    /// relations to <paramref name="ignoredDialogId"/> (a dialog about to be purged).
    /// </summary>
    Task ClearHasRelationsWhereNoneLeft(IEnumerable<Guid> dialogIds, Guid? ignoredDialogId, CancellationToken cancellationToken);
}
