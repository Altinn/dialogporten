using System.Diagnostics;
using System.Net;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public static class UseStatusCodePagesHandlers
{

    public static async Task FromFastEndpointResponses(StatusCodeContext statusCodeContext)
    {
        var context = statusCodeContext.HttpContext;
        var status = context.Response.StatusCode;

        if (await HandleEdgeCases(status, context)) return;

        var message = GetMessageFromEndpointSummary(context, status);
        var validationFailures = status switch
        {
            (int)HttpStatusCode.Forbidden => new Forbidden(message ?? "Forbidden").ToValidationResults(),
            _ => []
        };
        var problem = context.ResponseBuilder(validationFailures);
        if (problem == null)
        {
            Log.Error("Found no problem response for status {Code}. No body will be added.", status);
            return;
        }

        await Results.Problem(problem).ExecuteAsync(context);
    }

    private static async Task<bool> HandleEdgeCases(int status, HttpContext context)
    {
        switch (status)
        {
            case (int)HttpStatusCode.NotFound:
                // Handle router level 404's, which means the endpoint is missing.
                // We don't want the 404 response from FastEndpoints because those 404's = resource doesn't exist
                await Results.Problem(new ProblemDetails
                {
                    Title = "Endpoint not found.",
                    Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                    Status = (int)HttpStatusCode.NotFound,
                    Instance = context.Request.Path,
                    Extensions = { { "traceId", Activity.Current?.Id ?? context.TraceIdentifier } }
                }).ExecuteAsync(context);
                return true;
            default:
                return false;
        }
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
