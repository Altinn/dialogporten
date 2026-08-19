using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Authentication;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Authentication;

[Collection(nameof(WebApiTestCollectionFixture))]
public class AuthenticationTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    public static TheoryData<AuthenticationScenario, EndpointScenario> AuthenticationCases =>
        AuthenticationTestHelpers.BuildAuthenticationCases<IServiceownerApi>();

    [E2ETheory]
    [MemberData(nameof(AuthenticationCases))]
    public async Task Should_Return_401_With_Expected_WwwAuthenticate_Header(
        AuthenticationScenario authenticationScenario,
        EndpointScenario endpointScenario)
    {
        using var _ = Fixture.UseServiceOwnerTokenOverrides(tokenOverride: authenticationScenario.TokenOverride);

        var response = await AuthenticationTestHelpers.InvokeEndpointAsync(
            Fixture.ServiceownerApi, endpointScenario.Method, TestContext.Current.CancellationToken);
        var requestPath = response.RequestMessage!.RequestUri!.AbsolutePath ?? throw new UnreachableException();

        response.ShouldHaveStatusCode(HttpStatusCode.Unauthorized);

        response.Headers.Should().NotBeNull();
        var hasAuthenticateHeader = response.Headers.TryGetValues("WWW-Authenticate", out var authenticateHeaders);
        hasAuthenticateHeader.Should().BeTrue();

        var authenticateHeaderValue = string.Join(',', authenticateHeaders ?? []);
        authenticateHeaderValue.Should().Contain("Bearer");
        authenticateHeaderValue.Should().Contain(authenticationScenario.ExpectedAuthenticateHeaderFragment);

        response.Error.Should().NotBeNull();
        var errorContent = response.Error.Content;
        errorContent.Should().NotBeNull();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(errorContent, JsonSerializerOptions.Web);

        problemDetails.Should().NotBeNull();
        problemDetails.Code.Should().BeNull();
        problemDetails.Detail.Should().BeNull();
        problemDetails.Errors.Should().BeNull();
        problemDetails.Instance.Should().Be(requestPath);
        problemDetails.Status.Should().Be((int)HttpStatusCode.Unauthorized);
        problemDetails.StatusDescription.Should().BeNull();
        problemDetails.Title.Should().Be("Unauthorized.");
        problemDetails.TraceId.Should().NotBeNull();
        problemDetails.ValidationErrors.Should().BeNull();
    }
}
