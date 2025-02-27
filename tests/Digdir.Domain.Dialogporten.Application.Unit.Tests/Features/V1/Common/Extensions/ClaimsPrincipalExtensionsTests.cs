using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetAuthenticationLevel_Should_Parse_Idporten_Acr_Claim_For_Level3()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("acr", Constants.IdportenLoaSubstantial)
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(3, authenticationLevel);
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Parse_Idporten_Acr_Claim_For_Level4()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("acr", Constants.IdportenLoaHigh)
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(4, authenticationLevel);
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Parse_Altinn_Authlevel_First()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("acr", Constants.IdportenLoaHigh),
            new Claim("urn:altinn:authlevel", "5")
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(5, authenticationLevel);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Include_SystemUserIdentifier_From_AuthorizationDetails()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "[{\"type\":\"urn:altinn:systemuser\",\"systemuser_id\":[\"e3b87b08-dce6-4edd-8308-db887950a83b\"],\"systemuser_org\":{\"authority\":\"iso6523-actorid-upis\",\"ID\":\"0192:991825827\"},\"system_id\":\"1d81b874-f139-4842-bd0a-e5cc64319272\"}]")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Contains(identifyingClaims, c => c.Type == "urn:altinn:systemuser" && c.Value == "e3b87b08-dce6-4edd-8308-db887950a83b");
    }
}
