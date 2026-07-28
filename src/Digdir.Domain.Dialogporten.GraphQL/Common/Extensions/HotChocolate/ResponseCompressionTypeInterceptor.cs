using System.Reflection;
using Digdir.Library.Utils.AspNet;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Configurations;
using Microsoft.AspNetCore.Http.Features;

namespace Digdir.Domain.Dialogporten.GraphQL.Common.Extensions.HotChocolate;

internal sealed class ResponseCompressionTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration definition)
    {
        if (definition is not ObjectTypeConfiguration objectType)
            return;

        foreach (var field in objectType.Fields)
        {
            if (field.Member?.GetCustomAttribute<EnableResponseCompressionAttribute>() is null)
                continue;

            field.MiddlewareConfigurations.Insert(0, new FieldMiddlewareConfiguration(next => async ctx =>
            {
                var httpContext = ctx.Services.GetService<IHttpContextAccessor>()?.HttpContext;
                if (httpContext?.Features.Get<IHttpsCompressionFeature>() is { } feature)
                {
                    feature.Mode = HttpsCompressionMode.Compress;
                }

                await next(ctx);
            }, isRepeatable: false));
        }
    }
}
