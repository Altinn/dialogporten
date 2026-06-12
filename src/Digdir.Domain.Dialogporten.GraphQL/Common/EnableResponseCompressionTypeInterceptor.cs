using System.Reflection;
using Digdir.Library.Utils.AspNet;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.AspNetCore.Http.Features;

namespace Digdir.Domain.Dialogporten.GraphQL.Common;

/// <summary>
/// Walks each object field at schema-build time and, for resolvers decorated with
/// <see cref="EnableResponseCompressionAttribute"/>, appends a field middleware that sets
/// <see cref="IHttpsCompressionFeature.Mode"/> to <see cref="HttpsCompressionMode.Compress"/> so the
/// globally registered ResponseCompression middleware compresses the response over HTTPS. No
/// runtime reflection — attribute lookup happens once during schema build.
/// </summary>
internal sealed class EnableResponseCompressionTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is not ObjectTypeConfiguration objectTypeConfiguration)
        {
            return;
        }

        foreach (var field in objectTypeConfiguration.Fields)
        {
            var member = field.ResolverMember ?? field.Member;
            if (member?.GetCustomAttribute<EnableResponseCompressionAttribute>() is null)
            {
                continue;
            }

            field.MiddlewareConfigurations.Add(new FieldMiddlewareConfiguration(
                next => async context =>
                {
                    // Belt-and-braces: response compression only applies to read responses.
                    // Skip if a decorated resolver is ever invoked under a mutation/subscription.
                    if (context.Operation.Definition.Operation == OperationType.Query)
                    {
                        var httpContext = context.Services.GetRequiredService<IHttpContextAccessor>().HttpContext;
                        if (httpContext?.Features.Get<IHttpsCompressionFeature>() is { } feature)
                        {
                            feature.Mode = HttpsCompressionMode.Compress;
                        }
                    }
                    await next(context).ConfigureAwait(false);
                }));
        }
    }
}
