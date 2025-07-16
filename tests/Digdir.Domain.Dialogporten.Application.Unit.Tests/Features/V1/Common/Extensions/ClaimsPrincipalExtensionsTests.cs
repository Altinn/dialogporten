using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common.Extensions;

[SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
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
    public void GetAuthenticationLevel_Should_Treat_SystemUsers_As_Level3()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "[{\"type\":\"urn:altinn:systemuser\",\"systemuser_id\":[\"e3b87b08-dce6-4edd-8308-db887950a83b\"],\"systemuser_org\":{\"authority\":\"iso6523-actorid-upis\",\"ID\":\"0192:991825827\"},\"system_id\":\"1d81b874-f139-4842-bd0a-e5cc64319272\"}]")
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(3, authenticationLevel);
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

    [Fact]
    public void GetAuthenticationLevel_Should_Throw_UnreachableException_When_No_Authentication_Claims_Present()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("some_other_claim", "some_value")
        ]));

        // Act & Assert
        Assert.Throws<UnreachableException>(() => claimsPrincipal.GetAuthenticationLevel());
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Throw_ArgumentException_For_Unknown_Acr_Value()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("acr", "unknown_acr_value")
        ]));

        // Act & Assert
        Assert.Throws<ArgumentException>(() => claimsPrincipal.GetAuthenticationLevel());
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Handle_Invalid_Altinn_Authlevel_And_Fallback_To_Acr()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("urn:altinn:authlevel", "invalid_number"),
            new Claim("acr", Constants.IdportenLoaSubstantial)
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(3, authenticationLevel);
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Handle_Empty_Altinn_Authlevel_And_Fallback_To_Acr()
    {
        // Arrange
        var claimsPrincipal = new ClaimsIdentity([
            new Claim("urn:altinn:authlevel", ""),
            new Claim("acr", Constants.IdportenLoaHigh)
        ]);

        // Act
        var authenticationLevel = new ClaimsPrincipal(claimsPrincipal).GetAuthenticationLevel();

        // Assert
        Assert.Equal(4, authenticationLevel);
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Handle_Zero_Altinn_Authlevel()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("urn:altinn:authlevel", "0")
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(0, authenticationLevel);
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Handle_Large_Altinn_Authlevel()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("urn:altinn:authlevel", "999")
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(999, authenticationLevel);
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Handle_Multiple_Acr_Claims_Taking_First()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("acr", Constants.IdportenLoaSubstantial),
            new Claim("acr", Constants.IdportenLoaHigh)
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(3, authenticationLevel); // Should return first match
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Handle_Multiple_Altinn_Authlevel_Claims_Taking_First()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("urn:altinn:authlevel", "2"),
            new Claim("urn:altinn:authlevel", "4")
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(2, authenticationLevel); // Should return first match
    }

    [Fact]
    public void GetAuthenticationLevel_Should_Handle_Whitespace_In_Altinn_Authlevel()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("urn:altinn:authlevel", "  3  ")
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(3, authenticationLevel);
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    [InlineData("5")]
    [InlineData("10")]
    public void GetAuthenticationLevel_Should_Parse_Valid_Altinn_Authlevel_Values(string level)
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("urn:altinn:authlevel", level)
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(int.Parse(level), authenticationLevel);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Return_Empty_When_No_Authorization_Details()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("other_claim", "other_value")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Invalid_Json_In_Authorization_Details()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "invalid_json")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Empty_Authorization_Details()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Empty_Json_Array()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "[]")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Authorization_Details_Without_Systemuser_Type()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "[{\"type\":\"other_type\",\"some_id\":\"value\"}]")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Authorization_Details_With_Missing_SystemUser_Id()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "[{\"type\":\"urn:altinn:systemuser\",\"systemuser_org\":{\"authority\":\"iso6523-actorid-upis\",\"ID\":\"0192:991825827\"},\"system_id\":\"1d81b874-f139-4842-bd0a-e5cc64319272\"}]")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Authorization_Details_With_Empty_SystemUser_Id_Array()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "[{\"type\":\"urn:altinn:systemuser\",\"systemuser_id\":[],\"systemuser_org\":{\"authority\":\"iso6523-actorid-upis\",\"ID\":\"0192:991825827\"},\"system_id\":\"1d81b874-f139-4842-bd0a-e5cc64319272\"}]")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Authorization_Details_With_Null_SystemUser_Id()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "[{\"type\":\"urn:altinn:systemuser\",\"systemuser_id\":null,\"systemuser_org\":{\"authority\":\"iso6523-actorid-upis\",\"ID\":\"0192:991825827\"},\"system_id\":\"1d81b874-f139-4842-bd0a-e5cc64319272\"}]")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Empty_Claims_Collection()
    {
        // Arrange
        var claims = new List<Claim>();

        // Act
        var identifyingClaims = claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Include_Pid_Claims()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("pid", "12345678901")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Single(identifyingClaims);
        Assert.Contains(identifyingClaims, c => c.Type == "pid" && c.Value == "12345678901");
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Include_Consumer_Claims()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("consumer", "test-consumer")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Single(identifyingClaims);
        Assert.Contains(identifyingClaims, c => c.Type == "consumer" && c.Value == "test-consumer");
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Include_Supplier_Claims()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("supplier", "test-supplier")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Single(identifyingClaims);
        Assert.Contains(identifyingClaims, c => c.Type == "supplier" && c.Value == "test-supplier");
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Include_Acr_Claims()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("acr", Constants.IdportenLoaSubstantial)
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Single(identifyingClaims);
        Assert.Contains(identifyingClaims, c => c.Type == "acr" && c.Value == Constants.IdportenLoaSubstantial);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Include_Altinn_Claims()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("urn:altinn:authlevel", "4"),
            new Claim("urn:altinn:userid", "12345")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Equal(2, identifyingClaims.Count());
        Assert.Contains(identifyingClaims, c => c.Type == "urn:altinn:authlevel" && c.Value == "4");
        Assert.Contains(identifyingClaims, c => c.Type == "urn:altinn:userid" && c.Value == "12345");
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Order_Claims_By_Type()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("urn:altinn:userid", "12345"),
            new Claim("pid", "12345678901"),
            new Claim("consumer", "test-consumer"),
            new Claim("acr", Constants.IdportenLoaSubstantial)
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims().ToList();

        // Assert
        Assert.Equal(4, identifyingClaims.Count);
        Assert.Equal("acr", identifyingClaims[0].Type);
        Assert.Equal("consumer", identifyingClaims[1].Type);
        Assert.Equal("pid", identifyingClaims[2].Type);
        Assert.Equal("urn:altinn:userid", identifyingClaims[3].Type);
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Mixed_Claim_Types()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("pid", "12345678901"),
            new Claim("consumer", "test-consumer"),
            new Claim("authorization_details", "[{\"type\":\"urn:altinn:systemuser\",\"systemuser_id\":[\"system-user-id\"],\"systemuser_org\":{\"authority\":\"iso6523-actorid-upis\",\"ID\":\"0192:991825827\"},\"system_id\":\"1d81b874-f139-4842-bd0a-e5cc64319272\"}]"),
            new Claim("acr", Constants.IdportenLoaHigh),
            new Claim("urn:altinn:authlevel", "4"),
            new Claim("other_claim", "should_be_ignored")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Equal(5, identifyingClaims.Count());
        Assert.Contains(identifyingClaims, c => c.Type == "pid" && c.Value == "12345678901");
        Assert.Contains(identifyingClaims, c => c.Type == "consumer" && c.Value == "test-consumer");
        Assert.Contains(identifyingClaims, c => c.Type == "acr" && c.Value == Constants.IdportenLoaHigh);
        Assert.Contains(identifyingClaims, c => c.Type == "urn:altinn:systemuser" && c.Value == "system-user-id");
        Assert.Contains(identifyingClaims, c => c.Type == "urn:altinn:authlevel" && c.Value == "4");
        Assert.DoesNotContain(identifyingClaims, c => c.Type == "other_claim");
    }

    [Fact]
    public void GetIdentifyingClaims_Should_Handle_Malformed_Authorization_Details_Json()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("authorization_details", "[{\"type\":\"urn:altinn:systemuser\",\"systemuser_id\":[\"valid-id\"]},malformed_json]")
        ]));

        // Act
        var identifyingClaims = claimsPrincipal.Claims.GetIdentifyingClaims();

        // Assert
        Assert.Empty(identifyingClaims);
    }

    [Theory]
    [InlineData(Constants.IdportenLoaSubstantial, 3)]
    [InlineData(Constants.IdportenLoaHigh, 4)]
    public void GetAuthenticationLevel_Should_Map_Idporten_Acr_Values_Correctly(string acrValue, int expectedLevel)
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("acr", acrValue)
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(expectedLevel, authenticationLevel);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("3.14")]
    [InlineData("")]
    [InlineData("  ")]
    public void GetAuthenticationLevel_Should_Handle_Invalid_Altinn_Authlevel_Values(string invalidValue)
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("urn:altinn:authlevel", invalidValue),
            new Claim("acr", Constants.IdportenLoaSubstantial)
        ]));

        // Act
        var authenticationLevel = claimsPrincipal.GetAuthenticationLevel();

        // Assert
        Assert.Equal(3, authenticationLevel); // Should fallback to ACR claim
    }
}
