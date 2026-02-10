using Digdir.Library.Dialogporten.E2E.Common;
using Xunit;

namespace Digdir.Domain.Dialogporten.E2E.Cleanup.Tests;

public sealed class CleanupTests(CleanupFixture fixture)
    : E2ETestBase<CleanupFixture>(fixture), IClassFixture<CleanupFixture>
{
    [E2EFact]
    public void CleanupDialogs() => Fixture.PurgeE2ETestDialogs();
}
