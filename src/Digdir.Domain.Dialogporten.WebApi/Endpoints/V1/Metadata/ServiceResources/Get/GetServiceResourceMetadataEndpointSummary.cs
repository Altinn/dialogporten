using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Metadata.ServiceResources.Get;

public sealed class GetServiceResourceMetadataEndpointSummary : Summary<GetServiceResourceMetadataEndpoint>
{
    public GetServiceResourceMetadataEndpointSummary()
    {
        Summary = "Gets service resources currently in use in Dialogporten.";
        Description = "Returns public service resource metadata with related service owner, role, and access package metadata.";
        Responses[StatusCodes.Status200OK] = "Service resource metadata.";
    }
}
