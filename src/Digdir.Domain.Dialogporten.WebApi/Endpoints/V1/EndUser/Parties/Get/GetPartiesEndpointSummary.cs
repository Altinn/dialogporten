using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.EndUser.Parties.Get;

public sealed class GetPartiesEndpointSummary : Summary<GetPartiesEndpoint>
{
    public GetPartiesEndpointSummary()
    {
        Summary = "Gets the list of authorized parties for the end user";
        Description = """
                      Gets the list of authorized parties for the end user. For more information see the documentation (link TBD).
                      """;

        Responses[StatusCodes.Status200OK] = "The list of authorized parties for the end user";
    }
}
