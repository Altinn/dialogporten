using System.Net;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Authentication;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Authentication;

[Collection(nameof(WebApiTestCollectionFixture))]
public class AuthenticationTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    public static TheoryData<AuthenticationScenario, EndpointScenario> AuthenticationCases =>
        AuthenticationTestHelpers.BuildAuthenticationCases<IEnduserApi>();

    [E2ETheory]
    [MemberData(nameof(AuthenticationCases))]
    public async Task Should_Return_401_With_Expected_WwwAuthenticate_Header(
        AuthenticationScenario authenticationScenario,
        EndpointScenario endpointScenario)
    {
        using var _ = Fixture.UseEndUserTokenOverrides(tokenOverride: authenticationScenario.TokenOverride);

        var response = await AuthenticationTestHelpers.InvokeEndpointAsync(
            Fixture.EnduserApi.V1, endpointScenario.Method, TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);

        response.Headers.Should().NotBeNull();
        var hasAuthenticateHeader = response.Headers.TryGetValues("WWW-Authenticate", out var authenticateHeaders);
        hasAuthenticateHeader.Should().BeTrue();

        var authenticateHeaderValue = string.Join(',', authenticateHeaders ?? []);
        authenticateHeaderValue.Should().Contain("Bearer");
        authenticateHeaderValue.Should().Contain(authenticationScenario.ExpectedAuthenticateHeaderFragment);
    }
}
