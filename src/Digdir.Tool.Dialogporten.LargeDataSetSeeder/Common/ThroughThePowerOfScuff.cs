using Digdir.Domain.Dialogporten.Application.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

internal sealed class ThroughThePowerOfScuff : IUserResourceRegistry
{
    public static ThroughThePowerOfScuff Instance { get; } = new();

    private ThroughThePowerOfScuff() { }

    public Task<bool> CurrentUserIsOwner(string serviceResource, CancellationToken cancellationToken) =>
        Task.FromResult(true);

    public Task<IReadOnlyCollection<string>> GetCurrentUserResourceIds(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<string>>([]);

    public bool UserCanModifyResourceType(string serviceResourceType) => true;

    public bool IsCurrentUserServiceOwnerAdmin() => true;
}
