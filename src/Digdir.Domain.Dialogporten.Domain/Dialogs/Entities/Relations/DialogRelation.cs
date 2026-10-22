using Digdir.Library.Entity.Abstractions.Features.Creatable;
using Digdir.Library.Entity.Abstractions.Features.Immutable;

namespace Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;

/// <summary>
/// A typed relation between two dialogs, stored once in canonical form: <see cref="DialogId"/> is always the lower
/// of the two ids (in PostgreSQL uuid order), and <see cref="TypeId"/> is the type as seen from <see cref="DialogId"/>.
/// The canonical order is a storage detail only; it does not constrain which dialog is the parent.
/// </summary>
/// <remarks>
/// Deliberately not an aggregate child of <see cref="DialogEntity"/>: a relation change only bumps the dialog the
/// write goes through, while the aggregate machinery would bump (and emit events for) both.
/// </remarks>
public sealed class DialogRelation : IImmutableEntity, ICreatableEntity
{
    public Guid DialogId { get; set; }
    public Guid RelatedDialogId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public DialogRelationType.Values TypeId { get; set; }
    public DialogRelationType Type { get; set; } = null!;

    public DialogEntity Dialog { get; set; } = null!;
    public DialogEntity RelatedDialog { get; set; } = null!;

    /// <summary>
    /// Creates the canonical row for "<paramref name="relatedDialogId"/> is the <paramref name="type"/> of
    /// <paramref name="dialogId"/>".
    /// </summary>
    public static DialogRelation Create(Guid dialogId, Guid relatedDialogId, DialogRelationType.Values type) =>
        DialogIdComparer.Compare(dialogId, relatedDialogId) < 0
            ? new() { DialogId = dialogId, RelatedDialogId = relatedDialogId, TypeId = type }
            : new() { DialogId = relatedDialogId, RelatedDialogId = dialogId, TypeId = type.Invert() };

    /// <summary>
    /// The other dialog and the relation type, as seen from <paramref name="dialogId"/>.
    /// </summary>
    public (Guid OtherDialogId, DialogRelationType.Values Type) SeenFrom(Guid dialogId) =>
        dialogId == DialogId ? (RelatedDialogId, TypeId) : (DialogId, TypeId.Invert());
}

/// <summary>
/// Orders dialog ids the way PostgreSQL orders uuids (byte-wise, big-endian), which differs from <see cref="Guid.CompareTo(Guid)"/>.
/// </summary>
public static class DialogIdComparer
{
    public static int Compare(Guid x, Guid y)
    {
        Span<byte> a = stackalloc byte[16];
        Span<byte> b = stackalloc byte[16];
        x.TryWriteBytes(a, bigEndian: true, out _);
        y.TryWriteBytes(b, bigEndian: true, out _);
        return a.SequenceCompareTo(b);
    }
}
