using FastEndpoints;
using static Digdir.Domain.Dialogporten.WebApi.Common.Swagger.AuthorizationFailureMessageBuilder;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.AccessManagement.Queries.GetParties;

public sealed class GetPartiesEndpointSummary : Summary<GetPartiesEndpoint>
{
    public GetPartiesEndpointSummary()
    {
        Summary = "Gets the list of authorized parties for the end user";
        Description = """
                      Gets the list of authorized parties for the end user.
                      """;

        Responses[Status200OK] = "The list of authorized parties for the end user";
        Responses[Status401Unauthorized] = Constants.SwaggerSummary.AuthenticationFailure;
        Responses[Status403Forbidden] = DefaultForbiddenFor<GetPartiesEndpoint>().Build();
    }
}
