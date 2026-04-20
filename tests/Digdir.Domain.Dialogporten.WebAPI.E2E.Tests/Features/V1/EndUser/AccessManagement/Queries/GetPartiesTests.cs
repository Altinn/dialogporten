using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Digdir.Library.Dialogporten.E2E.Common.JsonSnapshotVerifier;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.AccessManagement.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetPartiesTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_Three_Authorized_Parties()
    {
        // Act
        var response = await Fixture.EnduserApi.V1EndUserAccessManagementQueriesGetPartiesParties(TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Parties content was null.");

        content.AuthorizedParties.Should().HaveCount(3);
        content.AuthorizedParties.Should().ContainSingle(x =>
            x.Party == E2EConstants.DefaultParty);
    }

    [E2EFact(SkipOnEnvironments = ["yt01", "staging"])]
    public async Task Authorized_Parties_Verify_Snapshot()
    {
        // Arrange/Act
        var response = await Fixture.EnduserApi.V1EndUserAccessManagementQueriesGetPartiesParties(TestContext.Current.CancellationToken);
        var content = response.Content ?? throw new InvalidOperationException("Parties content was null.");

        // Assert
        await VerifyJsonSnapshot(
            JsonSerializer.Serialize(content),
            scrubGuids: false);
    }
}
