using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Compliance;

public sealed class ComplianceGroup : Group
{
    private const string RoutePrefix = "compliance";
    public ComplianceGroup()
    {
        Configure(RoutePrefix, ep =>
        {
            ep.EndpointVersion(1);
        });
    }
}
