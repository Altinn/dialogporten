namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Auth;

public sealed record AuthorizationScenario(
    string Name,
    bool ShouldSucceed,
    string? OrgNumber = null,
    string? OrgName = null,
    string? Scopes = null)
{
    public override string ToString() => Name;
}

public sealed record AuthScenario(
    string Name,
    string TokenOverride,
    string ExpectedAuthenticateHeaderFragment)
{
    public override string ToString() => Name;
}
