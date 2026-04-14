using System.Reflection;
using System.Text.RegularExpressions;
using Digdir.Domain.Dialogporten.WebApi.Common;
using FastEndpoints;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Digdir.Domain.Dialogporten.WebApi;

internal static class OpenApiServiceCollectionExtensions
{
    internal static IServiceCollection AddOpenApi(this IServiceCollection services, string apiVersion, string audience)
    {
        EnsureUniqueOperationIds(audience);

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

                EnsurePathParameters(operation, context.Description.RelativePath);

                if (string.Equals(context.Description.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    operation.RequestBody = null;
                }

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

    private static void EnsureUniqueOperationIds(string audience)
    {
        var normalizedAudience = audience.ToLowerInvariant();

        var endpointTypes = WebApiAssemblyMarker.Assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Where(type => type.GetCustomAttribute<OpenApiOperationIdAttribute>() is not null)
            .Where(type => IsAudienceEndpoint(type, normalizedAudience))
            .ToList();

        var duplicates = endpointTypes
            .GroupBy(type => type.GetCustomAttribute<OpenApiOperationIdAttribute>()!.OperationId)
            .Where(group => group.Count() > 1)
            .ToList();

        if (duplicates.Count == 0)
            return;

        var details = string.Join(", ",
            duplicates.Select(duplicate => $"'{duplicate.Key}' on [{string.Join(", ", duplicate.Select(type => type.Name))}]"));

        throw new InvalidOperationException(
            $"Duplicate [OpenApiOperationId] values detected for {normalizedAudience}: {details}");
    }

    private static bool IsAudienceEndpoint(Type type, string normalizedAudience)
    {
        var ns = type.Namespace ?? "";
        return normalizedAudience switch
        {
            "enduser" => ns.Contains(".EndUser.", StringComparison.OrdinalIgnoreCase),
            "serviceowner" => ns.Contains(".ServiceOwner.", StringComparison.OrdinalIgnoreCase),
            _ => throw new InvalidOperationException(
                $"Unknown OpenAPI audience '{normalizedAudience}'. Expected 'enduser' or 'serviceowner'.")
        };
    }

    private static void EnsurePathParameters(OpenApiOperation operation, string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return;

        operation.Parameters ??= [];

        var existingPathParameterNames = operation.Parameters
            .Where(parameter => parameter.In == ParameterLocation.Path)
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in RouteParameterRegex.Matches(relativePath))
        {
            var parameterName = match.Groups[1].Value;
            if (!existingPathParameterNames.Add(parameterName))
                continue;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = parameterName,
                In = ParameterLocation.Path,
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                }
            });
        }
    }

    private static readonly Regex RouteParameterRegex = new(@"\{([^}:]+)(?::[^}]+)?\}", RegexOptions.Compiled);
}
