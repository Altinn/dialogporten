using System.Security.Claims;
using Digdir.Domain.Dialogporten.GraphQL.Common;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.AspNetCore.Http;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests;

public class UserTypeAndAcceptLanguageInterceptorTests
{
    private const string AuthenticationType = "Bearer";

    [Fact]
    public async Task Authenticated_user_with_unknown_user_type_throws_graphql_exception()
    {
        var interceptor = new UserTypeAndAcceptLanguageInterceptor();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims: [], AuthenticationType))
        };

        var ex = await Assert.ThrowsAsync<GraphQLException>(async () =>
            await interceptor.OnCreateAsync(context, null!, OperationRequestBuilder.New(),
                TestContext.Current.CancellationToken));

        var error = Assert.Single(ex.Errors);
        Assert.Equal("AUTH_USER_TYPE_UNKNOWN", error.Code);
    }

    [Fact]
    public async Task Authenticated_user_with_known_user_type_passes_through()
    {
        var interceptor = new UserTypeAndAcceptLanguageInterceptor();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                claims: [new Claim("pid", "22834498646")],
                AuthenticationType))
        };

        var exception = await Record.ExceptionAsync(async () =>
            await interceptor.OnCreateAsync(context, null!, OperationRequestBuilder.New(),
                TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }
}
