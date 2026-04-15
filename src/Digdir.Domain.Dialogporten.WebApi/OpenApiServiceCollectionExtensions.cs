// using System.Reflection;
// using System.Text.RegularExpressions;
// using Digdir.Domain.Dialogporten.WebApi.Common;
// using FastEndpoints;
// using Microsoft.AspNetCore.OpenApi;
// using Microsoft.OpenApi;
//
// namespace Digdir.Domain.Dialogporten.WebApi;
//
// internal static partial class OpenApiServiceCollectionExtensions
// {
//     internal static IServiceCollection AddOpenApi(this IServiceCollection services, string apiVersion, string audience)
//     {
//         EnsureUniqueOperationIds(audience);
//
//         return services.AddOpenApi($"{apiVersion}.{audience}", options =>
//         {
//             options.AddDocumentTransformer((doc, _, _) =>
//             {
//                 doc.Info.Version = apiVersion;
//                 return Task.CompletedTask;
//             });
//
//             options.ShouldInclude = description =>
//                 description.RelativePath?.Contains($"api/{apiVersion}/{audience}/",
//                     StringComparison.OrdinalIgnoreCase) == true;
//
//             options.AddOperationTransformer((operation, context, _) =>
//             {
//                 var attr = context.Description.ActionDescriptor.EndpointMetadata
//                     .OfType<OpenApiOperationIdAttribute>().FirstOrDefault();
//
//                 operation.OperationId = attr is not null
//                     ? attr.OperationId
//                     : throw new InvalidOperationException(
//                         $"Missing OpenApiOperationIdAttribute for {audience} endpoint " +
//                         $"{context.GetEndpointName() ?? "<unknown endpoint>"}.");
//
//                 EnsurePathParameters(operation, context.Description.RelativePath);
//                 EnsureGetOperationParameters(operation, context);
//
//                 if (string.Equals(context.Description.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
//                 {
//                     operation.RequestBody = null;
//                 }
//
//                 return Task.CompletedTask;
//             });
//         });
//     }
//
//     private static string? GetEndpointName(this OpenApiOperationTransformerContext context) =>
//         context.Description
//             .ActionDescriptor
//             .EndpointMetadata
//             .OfType<EndpointDefinition>()
//             .FirstOrDefault()?
//             .EndpointType?
//             .FullName?
//             .Split(".")[^1];
//
//     private static void EnsureUniqueOperationIds(string audience)
//     {
//         var normalizedAudience = audience.ToLowerInvariant();
//
//         var endpointTypes = WebApiAssemblyMarker.Assembly.GetTypes()
//             .Where(type => type is { IsClass: true, IsAbstract: false })
//             .Where(type => type.GetCustomAttribute<OpenApiOperationIdAttribute>() is not null)
//             .Where(type => IsAudienceEndpoint(type, normalizedAudience))
//             .ToList();
//
//         var duplicates = endpointTypes
//             .GroupBy(type => type.GetCustomAttribute<OpenApiOperationIdAttribute>()!.OperationId)
//             .Where(group => group.Count() > 1)
//             .ToList();
//
//         if (duplicates.Count == 0)
//             return;
//
//         var details = string.Join(", ",
//             duplicates.Select(duplicate => $"'{duplicate.Key}' on [{string.Join(", ", duplicate.Select(type => type.Name))}]"));
//
//         throw new InvalidOperationException(
//             $"Duplicate [OpenApiOperationId] values detected for {normalizedAudience}: {details}");
//     }
//
//     private static bool IsAudienceEndpoint(Type type, string normalizedAudience)
//     {
//         var ns = type.Namespace ?? "";
//         return normalizedAudience switch
//         {
//             "enduser" => ns.Contains(".EndUser.", StringComparison.OrdinalIgnoreCase),
//             "serviceowner" => ns.Contains(".ServiceOwner.", StringComparison.OrdinalIgnoreCase),
//             _ => throw new InvalidOperationException(
//                 $"Unknown OpenAPI audience '{normalizedAudience}'. Expected 'enduser' or 'serviceowner'.")
//         };
//     }
//
//     private static void EnsurePathParameters(OpenApiOperation operation, string? relativePath)
//     {
//         if (string.IsNullOrWhiteSpace(relativePath))
//             return;
//
//         operation.Parameters ??= [];
//
//         var existingPathParameterNames = operation.Parameters
//             .Where(parameter => parameter.In == ParameterLocation.Path)
//             .Select(parameter => parameter.Name)
//             .ToHashSet(StringComparer.OrdinalIgnoreCase);
//
//         foreach (Match match in RouteParameterRegex.Matches(relativePath))
//         {
//             var parameterName = match.Groups[1].Value;
//             if (!existingPathParameterNames.Add(parameterName))
//                 continue;
//
//             operation.Parameters.Add(new OpenApiParameter
//             {
//                 Name = parameterName,
//                 In = ParameterLocation.Path,
//                 Required = true,
//                 Schema = new OpenApiSchema
//                 {
//                     Type = JsonSchemaType.String
//                 }
//             });
//         }
//     }
//
//     private static readonly Regex RouteParameterRegex = MyRegex();
//
//     private static void EnsureGetOperationParameters(OpenApiOperation operation, OpenApiOperationTransformerContext context)
//     {
//         if (!string.Equals(context.Description.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
//             return;
//
//         var requestType = context.GetRequestType();
//         if (requestType is null)
//             return;
//
//         operation.Parameters ??= [];
//
//         var existingParameterNames = operation.Parameters
//             .Select(parameter => $"{parameter.In}:{parameter.Name}")
//             .ToHashSet(StringComparer.OrdinalIgnoreCase);
//
//         foreach (var property in requestType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
//         {
//             if (property.GetMethod is null || property.GetIndexParameters().Length > 0)
//                 continue;
//
//             var headerName = GetHeaderName(property);
//             if (headerName is not null)
//             {
//                 var key = $"{ParameterLocation.Header}:{headerName}";
//                 if (existingParameterNames.Add(key))
//                 {
//                     operation.Parameters.Add(new OpenApiParameter
//                     {
//                         Name = headerName,
//                         In = ParameterLocation.Header,
//                         Required = false,
//                         Schema = CreateSchema(property.PropertyType)
//                     });
//                 }
//
//                 continue;
//             }
//
//             if (operation.Parameters.Any(parameter =>
//                     parameter.In == ParameterLocation.Path &&
//                     string.Equals(parameter.Name, property.Name, StringComparison.OrdinalIgnoreCase)))
//             {
//                 continue;
//             }
//
//             var queryName = GetQueryName(property);
//             var queryKey = $"{ParameterLocation.Query}:{queryName}";
//             if (!existingParameterNames.Add(queryKey))
//                 continue;
//
//             operation.Parameters.Add(new OpenApiParameter
//             {
//                 Name = queryName,
//                 In = ParameterLocation.Query,
//                 Required = false,
//                 Schema = CreateSchema(property.PropertyType)
//             });
//         }
//     }
//
//     private static Type? GetRequestType(this OpenApiOperationTransformerContext context)
//     {
//         var endpointType = context.Description
//             .ActionDescriptor
//             .EndpointMetadata
//             .OfType<EndpointDefinition>()
//             .FirstOrDefault()?
//             .EndpointType;
//
//         var current = endpointType;
//         while (current is not null)
//         {
//             if (current.IsGenericType &&
//                 current.Namespace == "FastEndpoints" &&
//                 current.Name.StartsWith("Endpoint", StringComparison.Ordinal))
//             {
//                 return current.GetGenericArguments().FirstOrDefault();
//             }
//
//             current = current.BaseType;
//         }
//
//         return null;
//     }
//
//     private static string? GetHeaderName(PropertyInfo property)
//     {
//         var attribute = property.GetCustomAttributesData()
//             .FirstOrDefault(attr => attr.AttributeType.Name == "FromHeaderAttribute");
//         if (attribute is null)
//             return null;
//
//         if (attribute.ConstructorArguments.Count > 0 &&
//             attribute.ConstructorArguments[0].Value is string constructorHeaderName &&
//             !string.IsNullOrWhiteSpace(constructorHeaderName))
//         {
//             return constructorHeaderName;
//         }
//
//         var namedHeaderName = attribute.NamedArguments
//             .FirstOrDefault(arg => string.Equals(arg.MemberName, "HeaderName", StringComparison.OrdinalIgnoreCase))
//             .TypedValue.Value as string;
//
//         return string.IsNullOrWhiteSpace(namedHeaderName) ? property.Name : namedHeaderName;
//     }
//
//     private static string GetQueryName(PropertyInfo property)
//     {
//         var bindFromAttribute = property.GetCustomAttributesData()
//             .FirstOrDefault(attr => attr.AttributeType.Name == "BindFromAttribute");
//         if (bindFromAttribute is not null &&
//             bindFromAttribute.ConstructorArguments.Count > 0 &&
//             bindFromAttribute.ConstructorArguments[0].Value is string bindFromName &&
//             !string.IsNullOrWhiteSpace(bindFromName))
//         {
//             return bindFromName;
//         }
//
//         return property.Name;
//     }
//
//     private static OpenApiSchema CreateSchema(Type type)
//     {
//         var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
//
//         if (underlyingType != typeof(string) &&
//             underlyingType != typeof(byte[]) &&
//             typeof(System.Collections.IEnumerable).IsAssignableFrom(underlyingType))
//         {
//             var elementType = underlyingType.IsArray
//                 ? underlyingType.GetElementType()!
//                 : underlyingType.GetGenericArguments().FirstOrDefault() ?? typeof(string);
//
//             return new OpenApiSchema
//             {
//                 Type = JsonSchemaType.Array,
//                 Items = CreateSchema(elementType)
//             };
//         }
//
//         if (underlyingType.IsEnum)
//         {
//             return new OpenApiSchema
//             {
//                 Type = JsonSchemaType.String
//             };
//         }
//
//         return underlyingType switch
//         {
//             _ when underlyingType == typeof(Guid) => new OpenApiSchema { Type = JsonSchemaType.String, Format = "guid" },
//             _ when underlyingType == typeof(DateTime) || underlyingType == typeof(DateTimeOffset) =>
//                 new OpenApiSchema { Type = JsonSchemaType.String, Format = "date-time" },
//             _ when underlyingType == typeof(bool) => new OpenApiSchema { Type = JsonSchemaType.Boolean },
//             _ when underlyingType == typeof(int) || underlyingType == typeof(long) ||
//                    underlyingType == typeof(short) || underlyingType == typeof(byte) =>
//                 new OpenApiSchema { Type = JsonSchemaType.Integer },
//             _ when underlyingType == typeof(decimal) || underlyingType == typeof(double) || underlyingType == typeof(float) =>
//                 new OpenApiSchema { Type = JsonSchemaType.Number },
//             _ => new OpenApiSchema { Type = JsonSchemaType.String }
//         };
//     }
//
//     [GeneratedRegex(@"\{([^}:]+)(?::[^}]+)?\}", RegexOptions.Compiled)]
//     private static partial Regex MyRegex();
// }
