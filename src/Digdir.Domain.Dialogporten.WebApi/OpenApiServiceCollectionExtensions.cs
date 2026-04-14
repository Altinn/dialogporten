using System.Reflection;
using Digdir.Domain.Dialogporten.WebApi.Common;
using FastEndpoints;
using Microsoft.AspNetCore.OpenApi;

namespace Digdir.Domain.Dialogporten.WebApi;

internal static class OpenApiServiceCollectionExtensions
{
    private static readonly HashSet<string> AudiencesValidated = [];

    internal static IServiceCollection AddOpenApi(this IServiceCollection services, string apiVersion, string audience)
    {
        EnsureUniqueOperationIds();

        return services.AddOpenApi($"{apiVersion}.{audience}", options =>
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
    }

    private static string? GetEndpointName(this OpenApiOperationTransformerContext context) =>
        context.Description
            .ActionDescriptor
            .EndpointMetadata
            .OfType<EndpointDefinition>()
            .FirstOrDefault()?
            .EndpointType?
            .FullName?
            .Split(".")[^1];

    private static void EnsureUniqueOperationIds()
    {
        if (!AudiencesValidated.Add("all"))
            return;

        var endpointTypes = WebApiAssemblyMarker.Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetCustomAttribute<OpenApiOperationIdAttribute>() is not null)
            .ToList();

        var duplicates = endpointTypes
            .GroupBy(type => type.GetCustomAttribute<OpenApiOperationIdAttribute>()!.OperationId)
            .Where(group => group.Count() > 1)
            .ToList();

        if (duplicates.Count == 0)
            return;

        var details = string.Join(", ",
            duplicates.Select(duplicate => $"'{duplicate.Key}' on [{string.Join(", ", duplicate.Select(type => type.Name))}]"));

        throw new InvalidOperationException($"Duplicate [OpenApiOperationId] values detected: {details}");
    }
}
