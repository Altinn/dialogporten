using FastEndpoints;
using Microsoft.AspNetCore.Mvc.Controllers;
using NSwag.Generation.AspNetCore;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Swagger;

public sealed class OpenApiOperationIdOverrideProcessor(string documentName) : IOperationProcessor
{
    public bool Process(OperationProcessorContext context)
    {
        var aspNetContext = (AspNetCoreOperationProcessorContext)context;
        var operationId = ResolveOperationId(aspNetContext);
        context.OperationDescription.Operation.OperationId = operationId;

        return true;
    }

    private string ResolveOperationId(AspNetCoreOperationProcessorContext context)
    {
        var metadataOperationId = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<OpenApiOperationIdAttribute>()
            .LastOrDefault()
            ?.OperationId;

        if (!string.IsNullOrWhiteSpace(metadataOperationId))
        {
            return metadataOperationId;
        }

        var endpointTypeName = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointDefinition>()
            .SingleOrDefault()
            ?.EndpointType
            .Name
            ?? (context.ApiDescription.ActionDescriptor as ControllerActionDescriptor)
                ?.ControllerTypeInfo
                .Name
            ?? context.ApiDescription.ActionDescriptor.DisplayName
            ?? context.ApiDescription.RelativePath
            ?? "<unknown endpoint>";

        throw new InvalidOperationException(
            $"Missing {nameof(OpenApiOperationIdAttribute)} for document '{documentName}' on endpoint '{endpointTypeName}'.");
    }
}
