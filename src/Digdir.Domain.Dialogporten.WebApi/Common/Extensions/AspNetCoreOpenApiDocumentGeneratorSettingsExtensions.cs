using Digdir.Domain.Dialogporten.Application.Common.Pagination.Continuation;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.WebApi.Common.Json;
using NJsonSchema.Generation.TypeMappers;
using NJsonSchema;
using NSwag.Generation.AspNetCore;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

internal static class AspNetCoreOpenApiDocumentGeneratorSettingsExtensions
{
    public static AspNetCoreOpenApiDocumentGeneratorSettings CleanupPaginatedLists(
        this AspNetCoreOpenApiDocumentGeneratorSettings settings)
    {
        settings.OperationProcessors.Add(new PaginatedListParametersProcessor());

        // Prevent NSwag from fully expanding these generic types into complex schemas.
        // The resulting empty schema definitions are removed in PostProcess by RemoveUnusedPaginationSchemas.
        foreach (var ignoreType in new[]
        {
            typeof(ContinuationTokenSet<,>),
            typeof(Order<>),
            typeof(OrderSet<,>)
        })
        {
            settings.SchemaSettings.TypeMappers.Add(new ObjectTypeMapper(ignoreType, new JsonSchema { Type = JsonObjectType.None }));
        }

        return settings;
    }

    public static AspNetCoreOpenApiDocumentGeneratorSettings EnsureJsonPatchConsumes(
        this AspNetCoreOpenApiDocumentGeneratorSettings settings)
    {
        settings.OperationProcessors.Add(new JsonPatchConsumesOperationProcessor());
        return settings;
    }
}
