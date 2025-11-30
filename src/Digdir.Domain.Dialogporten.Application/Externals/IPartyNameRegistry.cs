namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IPartyNameRegistry
{
    Task<string?> GetName(string externalIdWithPrefix, CancellationToken cancellationToken);
    Task<string?> GetOrgName(string orgNumber, CancellationToken cancellationToken);
}
