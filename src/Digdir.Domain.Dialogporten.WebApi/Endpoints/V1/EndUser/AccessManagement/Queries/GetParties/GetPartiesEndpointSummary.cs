using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.AccessManagement.Queries.GetParties;

public sealed class GetPartiesEndpointSummary : Summary<GetPartiesEndpoint>
{
    public GetPartiesEndpointSummary()
    {
        Summary = "Gets the list of authorized parties for the end user";
        Description = """
                      Gets the list of authorized parties for the end user.
                      """;

        Responses[StatusCodes.Status200OK] = "The list of authorized parties for the end user";
    }
}
