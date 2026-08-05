namespace Digdir.Library.Utils.AspNet;

/// <summary>
/// Marks an endpoint (FastEndpoints) or resolver (HotChocolate) as eligible for response compression
/// over HTTPS. Each host wires its own discovery: WebApi via a FastEndpoints endpoint filter that sets
/// <see cref="Microsoft.AspNetCore.Http.Features.IHttpsCompressionFeature.Mode"/>; GraphQL via a
/// HotChocolate type interceptor that prepends a field middleware doing the same.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class EnableResponseCompressionAttribute : Attribute;
