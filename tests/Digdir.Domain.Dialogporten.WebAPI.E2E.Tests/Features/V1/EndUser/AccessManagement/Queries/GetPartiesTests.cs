using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.AccessManagement.Queries;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetPartiesTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_At_Least_Two_AuthorizedParties()
    {
        // Act
        var response = await Fixture.EnduserApi.V1EndUserAccessManagementQueriesGetPartiesParties(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Parties content was null.");
        content.Should().NotBeEmpty();

        content.Should().AllSatisfy(party =>
            party.AuthorizedParties.Should().HaveAtLeast(2));
    }
}
