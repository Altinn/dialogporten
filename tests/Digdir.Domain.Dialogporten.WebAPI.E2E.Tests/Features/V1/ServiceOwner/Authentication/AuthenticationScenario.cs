namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Authentication;

public sealed record AuthenticationScenario(
    string Name,
    string TokenOverride,
    string ExpectedAuthenticateHeaderFragment)
{
    public override string ToString() => Name;
}
