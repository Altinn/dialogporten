using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

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

    private readonly ITransmissionHierarchyRepository _hierarchyRepository;
    private readonly IDomainContext _domainContext;

    public TransmissionHierarchyValidator(
        ITransmissionHierarchyRepository hierarchyRepository,
        IDomainContext domainContext)
    {
        _hierarchyRepository = hierarchyRepository ?? throw new ArgumentNullException(nameof(hierarchyRepository));
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
        if (newTransmissions.Count == 0) return;

        var newIds = newTransmissions.Select(x => x.Id);

        var existingParentIds = newTransmissions
            .Select(x => x.RelatedTransmissionId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .Except(newIds)
            .ToArray();

        var existingNodes = existingParentIds.Length != 0
            ? await _hierarchyRepository.GetHierarchyNodes(dialogId, existingParentIds, cancellationToken)
            : [];

        var nodes = newTransmissions
            .Select(x => new TransmissionHierarchyNode(x.Id, x.RelatedTransmissionId))
            .Concat(existingNodes)
            .ToList();

        _domainContext.AddErrors(nodes.ValidateReferenceHierarchy(
            x => x.Id,
            x => x.ParentId,
            nameof(DialogEntity.Transmissions),
            MaxHierarchyDepth,
            MaxHierarchyWidth));
    }
}
