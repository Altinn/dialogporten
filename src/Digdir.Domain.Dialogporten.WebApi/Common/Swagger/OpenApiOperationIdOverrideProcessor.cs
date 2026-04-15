using System.Reflection;
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

        var endpointOperationId = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<EndpointDefinition>()
            .SingleOrDefault()
            ?.EndpointType
            .GetCustomAttribute<OpenApiOperationIdAttribute>()
            ?.OperationId;

        if (!string.IsNullOrWhiteSpace(endpointOperationId))
        {
            return endpointOperationId;
        }

        var controllerAction = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
        var controllerOperationId =
            controllerAction?.MethodInfo.GetCustomAttribute<OpenApiOperationIdAttribute>()?.OperationId
            ?? controllerAction?.ControllerTypeInfo.GetCustomAttribute<OpenApiOperationIdAttribute>()?.OperationId;

        if (!string.IsNullOrWhiteSpace(controllerOperationId))
        {
            return controllerOperationId;
        }

        var endpointDisplayName = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<RouteNameMetadata>()
            .LastOrDefault()
            ?.RouteName
            ?? context.ApiDescription.ActionDescriptor.DisplayName
            ?? context.ApiDescription.RelativePath
            ?? "<unknown endpoint>";

        throw new InvalidOperationException(
            $"Missing {nameof(OpenApiOperationIdAttribute)} for document '{documentName}' on endpoint '{endpointDisplayName}'.");
    }
}
