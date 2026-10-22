using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Relations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common.Relations;

public sealed record NewDialogRelation(Guid RelatedDialogId, DialogRelationType.Values Type);

/// <summary>
/// Adds and removes relations through one dialog (the "through" dialog), enforcing the relation rules. Must run in a
/// transaction (<see cref="IUnitOfWork.BeginTransactionAsync"/>), which is committed by the caller's
/// <see cref="IUnitOfWork.SaveChangesAsync"/>. Rule violations are reported to <see cref="IDomainContext"/>.
/// </summary>
/// <remarks>
/// Only the through dialog is bumped (UpdatedAt, Revision and dialog.updated, by being modified in change tracking).
/// The related dialogs only get HasRelations synced, directly in the database, so they are not bumped.
/// </remarks>
internal interface IDialogRelationWriter
{
    /// <param name="dialog">The tracked through dialog.</param>
    /// <param name="isNewDialog">Whether the dialog is being created, in which case it is not bumped.</param>
    /// <param name="relations">The relations to add, with the type as seen from <paramref name="dialog"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task Add(DialogEntity dialog, bool isNewDialog, IReadOnlyCollection<NewDialogRelation> relations, CancellationToken cancellationToken);

    /// <returns>false if the dialogs are not related.</returns>
    Task<bool> Remove(DialogEntity dialog, Guid relatedDialogId, CancellationToken cancellationToken);
}

internal sealed class DialogRelationWriter : IDialogRelationWriter
{
    private const string PropertyName = "Relations";
    private const int MaxRootLockAttempts = 5;

    private readonly IDialogDbContext _db;
    private readonly IDialogRelationRepository _repository;
    private readonly IDomainContext _domainContext;
    private readonly IClock _clock;
    private readonly IOptionsSnapshot<ApplicationSettings> _settings;

    public DialogRelationWriter(
        IDialogDbContext db,
        IDialogRelationRepository repository,
        IDomainContext domainContext,
        IClock clock,
        IOptionsSnapshot<ApplicationSettings> settings)
    {
        _db = db;
        _repository = repository;
        _domainContext = domainContext;
        _clock = clock;
        _settings = settings;
    }

    private DialogRelationLimits Limits => _settings.Value.Limits.DialogRelations;

    public async Task Add(DialogEntity dialog, bool isNewDialog, IReadOnlyCollection<NewDialogRelation> relations, CancellationToken cancellationToken)
    {
        if (relations.Count == 0)
        {
            return;
        }

        if (!ValidateRequest(dialog.Id, relations))
        {
            return;
        }

        var targetIds = relations.Select(x => x.RelatedDialogId).ToList();
        var existing = await _repository.LockDialogs(isNewDialog ? targetIds : [dialog.Id, .. targetIds], cancellationToken);

        foreach (var missing in targetIds.Where(x => !existing.Contains(x)))
        {
            _domainContext.AddError(PropertyName, $"Related dialog {missing} does not exist or is deleted.");
        }

        if (!_domainContext.IsValid)
        {
            return;
        }

        await ValidateCardinality(dialog.Id, isNewDialog, relations, cancellationToken);

        var hierarchical = relations.Where(x => x.Type is not DialogRelationType.Values.Related).ToList();
        if (_domainContext.IsValid && hierarchical.Count > 0)
        {
            await ValidateHierarchy(dialog.Id, isNewDialog, hierarchical, cancellationToken);
        }

        if (!_domainContext.IsValid)
        {
            return;
        }

        _db.DialogRelations.AddRange(relations.Select(x => DialogRelation.Create(dialog.Id, x.RelatedDialogId, x.Type)));

        foreach (var relatedDialogId in targetIds)
        {
            await _repository.SetHasRelations(relatedDialogId, true, cancellationToken);
        }

        dialog.HasRelations = true;
        if (!isNewDialog)
        {
            Bump(dialog);
        }
    }

    public async Task<bool> Remove(DialogEntity dialog, Guid relatedDialogId, CancellationToken cancellationToken)
    {
        await _repository.LockDialogs([dialog.Id, relatedDialogId], cancellationToken);

        var canonical = DialogRelation.Create(dialog.Id, relatedDialogId, DialogRelationType.Values.Related);
        var deleted = await _db.DialogRelations
            .Where(x => x.DialogId == canonical.DialogId && x.RelatedDialogId == canonical.RelatedDialogId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            return false;
        }

        await _repository.ClearHasRelationsWhereNoneLeft([relatedDialogId], ignoredDialogId: null, cancellationToken);

        if (await _repository.CountRelations(dialog.Id, 1, cancellationToken) == 0)
        {
            dialog.HasRelations = false;
        }

        Bump(dialog);
        return true;
    }

    private void Bump(DialogEntity dialog) =>
        // Modifying the tracked dialog makes the aggregate machinery bump UpdatedAt and Revision and emit
        // dialog.updated. ContentUpdatedAt is left alone, as a relation is not a content change.
        dialog.UpdatedAt = _clock.UtcNowOffset;

    private bool ValidateRequest(Guid dialogId, IReadOnlyCollection<NewDialogRelation> relations)
    {
        if (relations.Any(x => x.RelatedDialogId == dialogId))
        {
            _domainContext.AddError(PropertyName, "A dialog cannot be related to itself.");
        }

        foreach (var duplicate in relations.GroupBy(x => x.RelatedDialogId).Where(x => x.Count() > 1))
        {
            _domainContext.AddError(PropertyName, $"Dialog {duplicate.Key} is listed more than once. A pair of dialogs can only have one relation.");
        }

        if (relations.Count(x => x.Type is DialogRelationType.Values.Parent) > 1)
        {
            _domainContext.AddError(PropertyName, "A dialog can have at most one parent.");
        }

        return _domainContext.IsValid;
    }

    private async Task ValidateCardinality(Guid dialogId, bool isNewDialog, IReadOnlyCollection<NewDialogRelation> relations, CancellationToken cancellationToken)
    {
        var max = Limits.MaxRelationsPerDialog;

        if (!isNewDialog)
        {
            var canonicalPairs = relations
                .Select(x => DialogRelation.Create(dialogId, x.RelatedDialogId, x.Type))
                .Select(x => new { x.DialogId, x.RelatedDialogId })
                .ToList();
            var lowIds = canonicalPairs.Select(x => x.DialogId).ToList();
            var highIds = canonicalPairs.Select(x => x.RelatedDialogId).ToList();
            var alreadyRelated = await _db.DialogRelations
                .Where(x => lowIds.Contains(x.DialogId) && highIds.Contains(x.RelatedDialogId))
                .Select(x => new { x.DialogId, x.RelatedDialogId })
                .ToListAsync(cancellationToken);

            foreach (var pair in alreadyRelated.Where(canonicalPairs.Contains))
            {
                var other = pair.DialogId == dialogId ? pair.RelatedDialogId : pair.DialogId;
                _domainContext.AddError(PropertyName, $"The dialog is already related to dialog {other}.");
            }

            if (await _repository.CountRelations(dialogId, max, cancellationToken) + relations.Count > max)
            {
                _domainContext.AddError(PropertyName, $"A dialog can have at most {max} relations.");
            }
        }
        else if (relations.Count > max)
        {
            _domainContext.AddError(PropertyName, $"A dialog can have at most {max} relations.");
        }

        foreach (var relation in relations)
        {
            if (await _repository.CountRelations(relation.RelatedDialogId, max, cancellationToken) >= max)
            {
                _domainContext.AddError(PropertyName, $"Dialog {relation.RelatedDialogId} already has the maximum of {max} relations.");
            }
        }
    }

    /// <summary>
    /// Enforces at most one parent, no cycles, and the depth limit. Locks the root of every hierarchy involved, so
    /// that concurrent writes elsewhere in the same hierarchies serialize with this one.
    /// </summary>
    private async Task ValidateHierarchy(Guid dialogId, bool isNewDialog, List<NewDialogRelation> relations, CancellationToken cancellationToken)
    {
        var maxDepth = Limits.MaxHierarchyDepth;
        var newParent = relations.SingleOrDefault(x => x.Type is DialogRelationType.Values.Parent)?.RelatedDialogId;
        var newChildren = relations.Where(x => x.Type is DialogRelationType.Values.Child).Select(x => x.RelatedDialogId).ToList();

        // The walks are bounded one step beyond the limit, so a broken hierarchy can't make them run away.
        var walkLimit = maxDepth + 1;
        var lockedRoots = new HashSet<Guid>();
        List<Guid> dialogAncestors = [];
        List<Guid> parentAncestors = [];
        for (var attempt = 0; ; attempt++)
        {
            dialogAncestors = isNewDialog ? [] : await _repository.GetAncestors(dialogId, walkLimit, cancellationToken);
            parentAncestors = newParent is { } p ? await _repository.GetAncestors(p, walkLimit, cancellationToken) : [];

            // The new children must have no parent (checked below), so they are roots themselves and already locked.
            var roots = new[]
                {
                    dialogAncestors.LastOrDefault(dialogId),
                    newParent is { } parent ? parentAncestors.LastOrDefault(parent) : dialogId
                }
                .Where(x => !lockedRoots.Contains(x) && (!isNewDialog || x != dialogId))
                .ToList();

            if (roots.Count == 0)
            {
                break;
            }

            if (attempt == MaxRootLockAttempts)
            {
                _domainContext.AddError(PropertyName, "The hierarchy kept changing while the relation was validated. Please try again.");
                return;
            }

            await _repository.LockDialogs(roots, cancellationToken);
            lockedRoots.UnionWith(roots);
        }

        if (newParent is { } newParentId)
        {
            if (dialogAncestors.Count > 0)
            {
                _domainContext.AddError(PropertyName, "The dialog already has a parent. A dialog can have at most one parent.");
            }

            if (parentAncestors.Contains(dialogId))
            {
                _domainContext.AddError(PropertyName, $"Dialog {newParentId} is a descendant of the dialog, so it cannot be its parent.");
            }
        }

        foreach (var child in newChildren)
        {
            if ((await _repository.GetAncestors(child, 1, cancellationToken)).Count > 0)
            {
                _domainContext.AddError(PropertyName, $"Dialog {child} already has a parent. A dialog can have at most one parent.");
            }

            if (dialogAncestors.Contains(child) || parentAncestors.Contains(child))
            {
                _domainContext.AddError(PropertyName, $"Dialog {child} is an ancestor of the dialog, so it cannot be its child.");
            }
        }

        if (!_domainContext.IsValid)
        {
            return;
        }

        // Depth of the dialog (relations from its root down to it), and the height below it after the change.
        var depth = newParent is not null ? parentAncestors.Count + 1 : dialogAncestors.Count;
        var allowedHeight = maxDepth - depth;
        if (allowedHeight < 0)
        {
            _domainContext.AddError(PropertyName, $"Parent/child hierarchies can be at most {maxDepth} levels deep.");
            return;
        }

        var tooDeep = !isNewDialog && await _repository.HasDescendantsAt(dialogId, allowedHeight + 1, cancellationToken);
        foreach (var child in newChildren)
        {
            tooDeep |= await _repository.HasDescendantsAt(child, allowedHeight, cancellationToken);
        }

        if (tooDeep)
        {
            _domainContext.AddError(PropertyName, $"Parent/child hierarchies can be at most {maxDepth} levels deep.");
        }
    }
}
