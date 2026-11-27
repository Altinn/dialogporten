using Digdir.Library.Entity.Abstractions;

namespace Digdir.Domain.Dialogporten.Domain.ResourcePolicyInformation;

public sealed class ResourcePolicyInformation : IEntity
{
    public Guid Id { get; set; }
    public string Resource { get; set; } = null!;
    public int MinimumAuthenticationLevel { get; set; }
    public int? MinimumSystemUserAuthenticationLevel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
