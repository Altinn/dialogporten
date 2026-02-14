namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IPartyServiceAssociationRepository
{
    Task<Dictionary<string, HashSet<string>>> GetExistingServicesByParty(
        IReadOnlyCollection<string> parties,
        IReadOnlyCollection<string> services,
        CancellationToken cancellationToken);
}
