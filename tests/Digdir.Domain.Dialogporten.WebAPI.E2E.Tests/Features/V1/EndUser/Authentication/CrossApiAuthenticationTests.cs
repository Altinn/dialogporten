using System.Net;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Authentication;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Authentication;

[Collection(nameof(WebApiTestCollectionFixture))]
public class CrossApiAuthenticationTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    public static TheoryData<EndpointScenario> EndpointCases =>
        AuthenticationTestHelpers.BuildEndpointCases<IEnduserApi>();

    [E2ETheory]
    [MemberData(nameof(EndpointCases))]
    public async Task Enduser_Endpoints_Should_Return_403_Forbidden_For_Valid_ServiceOwner_Tokens(
        EndpointScenario endpointScenario)
    {
        var serviceOwnerToken = await TestTokenGenerator.GenerateTokenAsync(
            TokenKind.ServiceOwner,
            Fixture.Settings,
            TestContext.Current.CancellationToken);

        using var _ = Fixture.UseEndUserTokenOverrides(tokenOverride: serviceOwnerToken);

        var response = await AuthenticationTestHelpers.InvokeEndpointAsync(
            Fixture.EnduserApi, endpointScenario.Method, TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
    }
}
