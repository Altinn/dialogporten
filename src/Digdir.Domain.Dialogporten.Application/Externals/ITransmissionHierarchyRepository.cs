using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface ITransmissionHierarchyRepository
{
    Task<IReadOnlyCollection<TransmissionHierarchyNode>> GetHierarchyNodes(
        Guid dialogId,
        IReadOnlyCollection<Guid> startIds,
        CancellationToken cancellationToken);
}

public sealed class TransmissionHierarchyNode
{
    public Guid Id { get; init; }
    public Guid? ParentId { get; init; }
}
