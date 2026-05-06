using System.Security.Claims;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.GraphQL.Common;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests;

public class UserTypeValidationInterceptorTests
{
    private const string AuthenticationType = "Bearer";

    [Fact]
    public async Task Authenticated_user_with_unknown_user_type_throws_graphql_exception()
    {
        var interceptor = new UserTypeValidationInterceptor();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims: [], AuthenticationType))
        };

        var act = async () =>
            await interceptor.OnCreateAsync(context, null!, OperationRequestBuilder.New(),
                TestContext.Current.CancellationToken);

        var ex = await act.Should().ThrowAsync<GraphQLException>();
        ex.Which.Errors.Should().ContainSingle()
            .Which.Code.Should().Be("AUTH_USER_TYPE_UNKNOWN");
    }

    [Fact]
    public async Task Authenticated_user_with_known_user_type_passes_through()
    {
        var interceptor = new UserTypeValidationInterceptor();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                claims: [new Claim("pid", "22834498646")],
                AuthenticationType))
        };

        var act = async () =>
            await interceptor.OnCreateAsync(context, null!, OperationRequestBuilder.New(),
                TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }
}
