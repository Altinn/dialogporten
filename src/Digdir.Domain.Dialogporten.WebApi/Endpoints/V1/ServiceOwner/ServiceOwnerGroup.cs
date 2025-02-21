using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.ServiceOwner;

public sealed class ServiceOwnerGroup : Group
{
    public const string RoutePrefix = "serviceowner";
    public ServiceOwnerGroup()
    {
        Configure(RoutePrefix.ToLowerInvariant(), ep =>
        {
            ep.EndpointVersion(1);
        });
    }
}
