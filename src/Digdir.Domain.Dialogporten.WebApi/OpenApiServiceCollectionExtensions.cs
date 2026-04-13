using Digdir.Domain.Dialogporten.WebApi.Common;
using FastEndpoints;
using Microsoft.AspNetCore.OpenApi;

namespace Digdir.Domain.Dialogporten.WebApi;

internal static class OpenApiServiceCollectionExtensions
{
    internal static IServiceCollection AddOpenApi(this IServiceCollection services, string apiVersion, string audience) =>
        services.AddOpenApi($"{apiVersion}.{audience}", options =>
        {
            options.AddDocumentTransformer((doc, _, _) =>
            {
                doc.Info.Version = apiVersion;
                return Task.CompletedTask;
            });

            options.ShouldInclude = description =>
                description.RelativePath?.Contains($"api/{apiVersion}/{audience}/",
                    StringComparison.OrdinalIgnoreCase) == true;

            options.AddOperationTransformer((operation, context, _) =>
            {
                var attr = context.Description.ActionDescriptor.EndpointMetadata
                    .OfType<OpenApiOperationIdAttribute>().FirstOrDefault();

                operation.OperationId = attr is not null
                    ? attr.OperationId
                    : throw new InvalidOperationException(
                        $"Missing OpenApiOperationIdAttribute for {audience} endpoint " +
                        $"{context.GetEndpointName() ?? "<unknown endpoint>"}.");

                return Task.CompletedTask;
            });
        });

    private static string? GetEndpointName(this OpenApiOperationTransformerContext context) =>
        context.Description
            .ActionDescriptor
            .EndpointMetadata
            .OfType<EndpointDefinition>()
            .FirstOrDefault()?
            .EndpointType?
            .FullName?
            .Split(".")[^1];
}
