using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Microsoft.EntityFrameworkCore;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Common;

internal interface ITransmissionHierarchyValidator
{
    void ValidateWholeAggregate(DialogEntity dialog);

    Task ValidateNewTransmissionsAsync(
        Guid dialogId,
        IReadOnlyCollection<DialogTransmission> newTransmissions,
        CancellationToken cancellationToken);
}

internal sealed class TransmissionHierarchyValidator : ITransmissionHierarchyValidator
{
    private const int MaxHierarchyDepth = 20;
    private const int MaxHierarchyWidth = 20;

    private readonly IDialogDbContext _dbContext;
    private readonly IDomainContext _domainContext;

    public TransmissionHierarchyValidator(
        IDialogDbContext dbContext,
        IDomainContext domainContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
    }

    public void ValidateWholeAggregate(DialogEntity dialog)
    {
        _domainContext.AddErrors(dialog.Transmissions.ValidateReferenceHierarchy(
            x => x.Id,
            x => x.RelatedTransmissionId,
            nameof(DialogEntity.Transmissions),
            MaxHierarchyDepth,
            MaxHierarchyWidth));
    }

    public async Task ValidateNewTransmissionsAsync(
        Guid dialogId,
        IReadOnlyCollection<DialogTransmission> newTransmissions,
        CancellationToken cancellationToken)
    {
        if (newTransmissions.Count == 0)
        {
            return;
        }

        var nodes = new Dictionary<Guid, TransmissionHierarchyNode>();

        foreach (var transmission in newTransmissions)
        {
            nodes.TryAdd(transmission.Id, new TransmissionHierarchyNode(transmission.Id, transmission.RelatedTransmissionId));
        }

        var parentIds = newTransmissions
            .Select(x => x.RelatedTransmissionId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToHashSet();

        var newIds = newTransmissions
            .Select(x => x.Id)
            .ToHashSet();

        var existingParentIds = parentIds
            .Where(x => !newIds.Contains(x))
            .ToHashSet();

        if (existingParentIds.Count != 0)
        {
            foreach (var node in await LoadHierarchyNodes(dialogId, existingParentIds, cancellationToken))
            {
                nodes.TryAdd(node.Id, node);
            }

            foreach (var node in await LoadExistingChildNodes(dialogId, existingParentIds, cancellationToken))
            {
                nodes.TryAdd(node.Id, node);
            }
        }

        _domainContext.AddErrors(nodes.Values.ToList().ValidateReferenceHierarchy(
            x => x.Id,
            x => x.ParentId,
            nameof(DialogEntity.Transmissions),
            MaxHierarchyDepth,
            MaxHierarchyWidth));
    }

    private async Task<List<TransmissionHierarchyNode>> LoadHierarchyNodes(
        Guid dialogId,
        IReadOnlyCollection<Guid> startIds,
        CancellationToken cancellationToken)
    {
        var nodes = new Dictionary<Guid, TransmissionHierarchyNode>();
        var pending = new Queue<Guid>(startIds);

        while (pending.Count > 0)
        {
            var batch = new HashSet<Guid>();
            while (pending.Count > 0 && batch.Count < 128)
            {
                var candidate = pending.Dequeue();
                if (nodes.ContainsKey(candidate))
                {
                    continue;
                }
                batch.Add(candidate);
            }

            if (batch.Count == 0)
            {
                continue;
            }

            var results = await _dbContext.DialogTransmissions
                .Where(x => x.DialogId == dialogId && batch.Contains(x.Id))
                .Select(x => new TransmissionHierarchyNode(x.Id, x.RelatedTransmissionId))
                .ToListAsync(cancellationToken);

            foreach (var result in results)
            {
                if (nodes.TryAdd(result.Id, result) && result.ParentId.HasValue && !nodes.ContainsKey(result.ParentId.Value))
                {
                    pending.Enqueue(result.ParentId.Value);
                }
            }
        }

        return nodes.Values.ToList();
    }

    private Task<List<TransmissionHierarchyNode>> LoadExistingChildNodes(
        Guid dialogId,
        HashSet<Guid> parentIds,
        CancellationToken cancellationToken)
    {
        if (parentIds.Count == 0)
        {
            return Task.FromResult(new List<TransmissionHierarchyNode>());
        }

        return _dbContext.DialogTransmissions
            .Where(x => x.DialogId == dialogId &&
                        x.RelatedTransmissionId.HasValue &&
                        parentIds.Contains(x.RelatedTransmissionId.Value))
            .Select(x => new TransmissionHierarchyNode(x.Id, x.RelatedTransmissionId))
            .ToListAsync(cancellationToken);
    }

    private sealed record TransmissionHierarchyNode(Guid Id, Guid? ParentId);
}
