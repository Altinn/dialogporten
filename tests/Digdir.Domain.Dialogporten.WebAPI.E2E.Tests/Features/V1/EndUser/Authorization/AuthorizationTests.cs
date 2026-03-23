using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Authentication;
using Digdir.Library.Dialogporten.E2E.Common;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Authorization;

[Collection(nameof(WebApiTestCollectionFixture))]
public class AuthorizationTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    public static TheoryData<EndpointScenario> AllEndUserEndpoints =>
        new(AuthenticationTestHelpers.GetEndpointScenarios<IEnduserApi>());

    [E2ETheory]
    [MemberData(nameof(AllEndUserEndpoints))]
    public async Task Should_Return_Forbidden_Without_EndUser_Scope(EndpointScenario endpointScenario)
    {
        using var _ = Fixture.UseEndUserTokenOverrides(scopes: "wrong-scope");

        var response = await AuthenticationTestHelpers.InvokeEndpointAsync(
            Fixture.EnduserApi, endpointScenario.Method, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
