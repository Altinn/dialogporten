using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Authentication;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Authentication;

[Collection(nameof(WebApiTestCollectionFixture))]
public class CrossApiAuthenticationTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    public static TheoryData<EndpointScenario> EndpointCases =>
        AuthenticationTestHelpers.BuildEndpointCases<IServiceownerApi>();

    [E2ETheory]
    [MemberData(nameof(EndpointCases))]
    public async Task ServiceOwner_Endpoints_Should_Return_403_Forbidden_For_Valid_EndUser_Tokens(
        EndpointScenario endpointScenario)
    {
        var endUserToken = await TestTokenGenerator.GenerateTokenAsync(
            TokenKind.EndUser,
            Fixture.Settings,
            TestContext.Current.CancellationToken);

        using var _ = Fixture.UseServiceOwnerTokenOverrides(tokenOverride: endUserToken);

        var response = await AuthenticationTestHelpers.InvokeEndpointAsync(
            Fixture.ServiceownerApi, endpointScenario.Method, TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
    }
}
