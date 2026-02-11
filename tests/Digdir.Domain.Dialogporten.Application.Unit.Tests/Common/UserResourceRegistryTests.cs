using System.Diagnostics;
using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Microsoft.Extensions.Logging;
using NSubstitute;
using AwesomeAssertions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common;

public sealed class UserResourceRegistryTests
{
    [Fact]
    public async Task GetCurrentUserOrgShortName_WhenConfiguredToWarn_LogsAndReturnsFirstShortName()
    {
        const string orgNumber = "910258028";
        var user = CreateUserWithConsumerOrgNumber(orgNumber);
        var resourceRegistry = CreateResourceRegistryWithShortNames(orgNumber, "org1", "org2");
        var logger = new TestLogger<UserResourceRegistry>();
        var options = new OptionsMock<ApplicationSettings>(CreateApplicationSettings(BadDataHandling.WarnAndContinue));

        var sut = new UserResourceRegistry(user, resourceRegistry, logger, options);

        var result = await sut.GetCurrentUserOrgShortName(CancellationToken.None);

        result.Should().Be("org1");
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == LogLevel.Warning &&
            entry.Message.Contains(orgNumber, StringComparison.Ordinal) &&
            entry.Message.Contains("org1", StringComparison.Ordinal) &&
            entry.Message.Contains("org2", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetCurrentUserOrgShortName_WhenConfiguredToThrow_Throws()
    {
        const string orgNumber = "910258028";
        var user = CreateUserWithConsumerOrgNumber(orgNumber);
        var resourceRegistry = CreateResourceRegistryWithShortNames(orgNumber, "org1", "org2");
        var logger = new TestLogger<UserResourceRegistry>();
        var options = new OptionsMock<ApplicationSettings>(CreateApplicationSettings(BadDataHandling.Throw));

        var sut = new UserResourceRegistry(user, resourceRegistry, logger, options);

        await Assert.ThrowsAsync<UnreachableException>(() =>
            sut.GetCurrentUserOrgShortName(CancellationToken.None));
    }

    private static IUser CreateUserWithConsumerOrgNumber(string orgNumber)
    {
        var claimValue = $"{{\"authority\":\"iso6523-actorid-upis\",\"ID\":\"0192:{orgNumber}\"}}";
        var claims = new[] { new Claim(ClaimsPrincipalExtensions.ConsumerClaim, claimValue) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var user = Substitute.For<IUser>();
        user.GetPrincipal().Returns(principal);
        return user;
    }

    private static IResourceRegistry CreateResourceRegistryWithShortNames(string orgNumber, params string[] shortNames)
    {
        var entries = shortNames
            .Select((name, index) => new ServiceResourceInformation(
                $"res{index + 1}",
                "type",
                orgNumber,
                name))
            .ToArray();

        var resourceRegistry = Substitute.For<IResourceRegistry>();
        resourceRegistry
            .GetResourceInformationForOrg(orgNumber, Arg.Any<CancellationToken>())
            .Returns(entries);

        return resourceRegistry;
    }

    private static ApplicationSettings CreateApplicationSettings(BadDataHandling handling) => new()
    {
        BadDataHandling = handling,
        Dialogporten = new DialogportenSettings
        {
            BaseUri = new Uri("https://example.test/dialogporten"),
            Ed25519KeyPairs = new Ed25519KeyPairs
            {
                Primary = new Ed25519KeyPair
                {
                    Kid = "primary",
                    PrivateComponent = "private",
                    PublicComponent = "public"
                },
                Secondary = new Ed25519KeyPair
                {
                    Kid = "secondary",
                    PrivateComponent = "private",
                    PublicComponent = "public"
                }
            }
        }
    };
}
