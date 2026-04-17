using System.Net;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Authentication;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

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
            Fixture.EnduserApi.V1, endpointScenario.Method, TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
    }
}
