using System.Security.Claims;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common;

public sealed class UserRegistryTests
{
    [Fact]
    public void GetCurrentUserId_WhenExternalIdIsNotFound_ThrowsWithPrincipalDiagnostics()
    {
        const string sensitiveClaimValue = "sensitive-value";
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimsPrincipalExtensions.IdportenAuthLevelClaim, "Level4"),
            new Claim(ClaimsPrincipalExtensions.ScopeClaim, "dialogporten:enduser"),
            new Claim("custom_claim", sensitiveClaimValue)
        ], "Bearer"));
        var user = Substitute.For<IUser>();
        user.GetPrincipal().Returns(principal);
        var partyNameRegistry = Substitute.For<IPartyNameRegistry>();
        var sut = new UserRegistry(user, partyNameRegistry);

        var exception = Assert.Throws<InvalidOperationException>(sut.GetCurrentUserId);

        exception.Message.Should().Contain("User external id not found.");
        exception.Message.Should().Contain("IsAuthenticated=True");
        exception.Message.Should().Contain("AuthenticatedIdentityCount=1");
        exception.Message.Should().Contain("AuthenticationTypes=Bearer");
        exception.Message.Should().Contain("ClaimTypes=acr, custom_claim, scope");
        exception.Message.Should().Contain("ClaimCount=3");
        exception.Message.Should().NotContain(sensitiveClaimValue);
    }
}
