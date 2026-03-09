namespace Digdir.Domain.Dialogporten.Application.Externals;

public interface IServiceOwnerNameRegistry
{
    Task<ServiceOwnerInfo?> GetServiceOwnerInfo(string orgNumber, CancellationToken cancellationToken);
}

public sealed class ServiceOwnerInfo
{
    public required string OrgNumber { get; init; }
    public required string ShortName { get; init; }
    public List<ResourceLocalization> DisplayName { get; init; } = [];
}
