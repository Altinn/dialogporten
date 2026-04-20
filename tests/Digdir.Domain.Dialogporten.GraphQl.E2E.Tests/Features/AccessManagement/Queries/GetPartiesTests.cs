using System.Text.Json;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using static Digdir.Library.Dialogporten.E2E.Common.JsonSnapshotVerifier;

namespace Digdir.Domain.Dialogporten.GraphQl.E2E.Tests.Features.AccessManagement.Queries;

[Collection(nameof(GraphQlTestCollectionFixture))]
public class GetPartiesTests(GraphQlE2EFixture fixture) : E2ETestBase<GraphQlE2EFixture>(fixture)
{
    [E2EFact(SkipOnEnvironments = ["yt01", "staging"])]
    public async Task Authorized_Parties_Verify_Snapshot()
    {
        // Act
        var result = await Fixture.GraphQlClient
            .GetAuthorizedParties
            .ExecuteAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Errors.Should().BeNullOrEmpty();
        result.Data.Should().NotBeNull();

        await VerifyJsonSnapshot(
            JsonSerializer.Serialize(result.Data),
            scrubGuids: false);
    }
}
