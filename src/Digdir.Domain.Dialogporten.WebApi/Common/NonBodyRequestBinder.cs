using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public static class NonBodyRequestBinder
{
    public static bool ShouldUseFor(EndpointDefinition endpointDefinition)
        => endpointDefinition.ReqDtoType != typeof(EmptyRequest)
           && endpointDefinition.Verbs.Length > 0
           && endpointDefinition.Verbs.All(IsNonBodyVerb);

    private static bool IsNonBodyVerb(string verb)
        => HttpMethods.IsGet(verb)
           || HttpMethods.IsHead(verb)
           || HttpMethods.IsDelete(verb);
}

public sealed class NonBodyRequestBinder<TRequest>() :
    RequestBinder<TRequest>(NonBodySources)
    where TRequest : notnull
{
    private const BindingSource NonBodySources =
        BindingSource.RouteValues |
        BindingSource.QueryParams |
        BindingSource.Headers |
        BindingSource.UserClaims |
        BindingSource.Permissions |
        BindingSource.Cookies;
}
