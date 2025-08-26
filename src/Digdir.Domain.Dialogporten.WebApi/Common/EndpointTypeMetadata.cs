namespace Digdir.Domain.Dialogporten.WebApi.Common;

/// <summary>
/// Metadata added to ASP.NET endpoints containing the concrete FastEndpoints endpoint type.
/// Enables downstream components (e.g., middleware) to reflect on endpoint attributes.
/// </summary>
public sealed class EndpointTypeMetadata
{
    public Type EndpointType { get; }

    public EndpointTypeMetadata(Type endpointType)
    {
        EndpointType = endpointType ?? throw new ArgumentNullException(nameof(endpointType));
    }
}
