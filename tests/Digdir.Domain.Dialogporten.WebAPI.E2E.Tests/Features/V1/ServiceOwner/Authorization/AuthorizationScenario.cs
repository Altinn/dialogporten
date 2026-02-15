namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Authorization;

public sealed record AuthorizationScenario(
    string Name,
    bool ShouldSucceed,
    string? OrgNumber = null,
    string? OrgName = null,
    string? Scopes = null)
{
    public override string ToString() => Name;
}
