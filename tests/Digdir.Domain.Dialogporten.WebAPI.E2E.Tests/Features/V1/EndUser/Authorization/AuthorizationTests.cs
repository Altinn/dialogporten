using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
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
            Fixture.EndUserApi.V1, endpointScenario.Method, TestContext.Current.CancellationToken);
        var requestPath = response.RequestMessage!.RequestUri!.AbsolutePath ?? throw new UnreachableException();

        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
        response.Error.Should().NotBeNull();
        var errorContent = response.Error.Content;
        errorContent.Should().NotBeNull();
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(errorContent, JsonSerializerOptions.Web);

        problemDetails.Should().NotBeNull();
        problemDetails.Code.Should().BeNull();
        problemDetails.Detail.Should().BeNull();
        problemDetails.Instance.Should().Be(requestPath);
        problemDetails.Status.Should().Be((int)HttpStatusCode.Forbidden);
        problemDetails.StatusDescription.Should().BeNull();
        problemDetails.Title.Should().Be("Forbidden.");
        problemDetails.TraceId.Should().NotBeNull();
        problemDetails.ValidationErrors.Should().BeNull();

        problemDetails.Errors.Should().NotBeNull();
        var validationFailure = problemDetails.Errors.Single();

        validationFailure.Key.Should().Be("Forbidden");
        validationFailure.Value.Should().NotBeNull();
    }
}
