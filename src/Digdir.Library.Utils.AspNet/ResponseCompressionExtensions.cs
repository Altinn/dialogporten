using System.IO.Compression;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Library.Utils.AspNet;

public static class ResponseCompressionExtensions
{
    public static IServiceCollection AddDialogportenResponseCompression(this IServiceCollection services)
    {
        services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
        services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

        services.AddResponseCompression(o =>
        {
            // Off-by-default for HTTPS; endpoints/resolvers opt in by applying
            // [EnableResponseCompression], which causes IHttpsCompressionFeature.Mode to be set
            // to Compress before the response body is written. Targeted surfaces return public,
            // non-user-specific data, so BREACH/CRIME side-channels do not apply.
            o.EnableForHttps = false;
            o.Providers.Add<BrotliCompressionProvider>();
            o.Providers.Add<GzipCompressionProvider>();
            o.MimeTypes =
            [
                "application/json",
                "application/problem+json",
                // GraphQL-over-HTTP spec; HotChocolate emits this when clients send the matching
                // Accept header (modern Apollo and graphql-fetch implementations do).
                "application/graphql-response+json",
            ];
        });
        return services;
    }
}
