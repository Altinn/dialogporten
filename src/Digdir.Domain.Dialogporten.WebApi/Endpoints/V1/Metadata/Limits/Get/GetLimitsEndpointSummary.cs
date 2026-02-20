using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Metadata.Limits.Get;

public sealed class GetLimitsEndpointSummary : Summary<GetLimitsEndpoint>
{
    public GetLimitsEndpointSummary()
    {
        Summary = "Gets currently enforced application-level query limits";
        Description = "Returns the active limits for EndUser and ServiceOwner search filters.";
        Responses[StatusCodes.Status200OK] = "The currently enforced application-level query limits.";
    }
}
