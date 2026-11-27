using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Externals;

public class MinimumAuthenticationLevelsTests
{
    [Theory]
    [InlineData(4, null, false, 4)]
    [InlineData(4, 3, false, 4)] // the system-user level never applies to end users
    [InlineData(4, null, true, 4)]
    [InlineData(4, 3, true, 3)]  // the system-user level relaxes the general level
    [InlineData(3, 4, true, 3)]  // a stricter system-user level does not tighten the general level
    [InlineData(2, 4, true, 2)]
    [InlineData(4, 4, true, 4)]
    public void RequiredFor_Should_Let_SystemUser_Level_Only_Relax_General_Level(
        int authenticationLevel,
        int? systemUserAuthenticationLevel,
        bool isSystemUser,
        int expected) =>
        new MinimumAuthenticationLevels(authenticationLevel, systemUserAuthenticationLevel)
            .RequiredFor(isSystemUser)
            .Should().Be(expected);
}
