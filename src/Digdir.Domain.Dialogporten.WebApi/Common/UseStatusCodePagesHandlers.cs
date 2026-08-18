using System.Net;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public static class UseStatusCodePagesHandlers
{

    public static async Task FromFastEndpointResponses(StatusCodeContext statusCodeContext)
    {
        var context = statusCodeContext.HttpContext;
        var status = context.Response.StatusCode;
        var message = GetMessageFromEndpointSummary(context, status);
        var failures = status switch
        {
            (int)HttpStatusCode.Forbidden => new Forbidden(message ?? "Forbidden").ToValidationResults(),
            _ => []
        };
        var problem = context.ResponseBuilder(failures);
        if (problem == null)
        {
            Log.Error("Found no problem response for status {Code}. No body will be added.", status);
            return;
        }

        await Results.Problem(problem).ExecuteAsync(context);
    }

    private static string? GetMessageFromEndpointSummary(HttpContext context, int statusCode)
    {
        var endpointMetadata = context.GetEndpoint()?.Metadata;
        var displayUrl = context.Request.PathBase + context.Request.Path;
        if (endpointMetadata == null)
        {
            Log.Warning("Found no Endpoint metadata for request url {Url}.", displayUrl);
            return null;
        }

        foreach (var entry in endpointMetadata)
        {
            if (entry is not EndpointDefinition endpointDefinition) continue;

            var endpointDefinitionEndpointSummary = endpointDefinition.EndpointSummary;
            var type = endpointDefinition.EndpointType;
            if (endpointDefinitionEndpointSummary == null)
            {
                Log.Warning("Found no EndpointSummary for endpoint {Type}.", type);
                return null;
            }
            var message = endpointDefinitionEndpointSummary.Responses.GetValueOrDefault(statusCode);

            if (message != null) return message;

            Log.Warning("Found no {Code} message in EndpointSummary for endpoint {Type}.", statusCode, type);
            return null;
        }

        Log.Warning("Found no EndpointDefinition for request url {Url}.", displayUrl);
        return null;
    }
}
