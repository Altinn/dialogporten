using System.Diagnostics;
using System.Net;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public static class UseStatusCodePagesHandlers
{
    public static async Task CreateStatusCodePageProblemDetails(StatusCodeContext statusCodeContext)
    {
        var context = statusCodeContext.HttpContext;
        var status = context.Response.StatusCode;

        var problem = status switch
        {
            (int)HttpStatusCode.Unauthorized => new ValidationProblemDetails(Get401ErrorMessages())
            {
                Title = "Unauthorized.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                Status = (int)HttpStatusCode.Unauthorized,
                Instance = context.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? context.TraceIdentifier } }
            },
            (int)HttpStatusCode.Forbidden => new ValidationProblemDetails(Get403ErrorMessages(context))
            {
                Title = "Forbidden.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                Status = (int)HttpStatusCode.Forbidden,
                Instance = context.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? context.TraceIdentifier } }
            },
            (int)HttpStatusCode.NotFound => new ProblemDetails
            {
                Title = "Endpoint not found.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                Status = (int)HttpStatusCode.NotFound,
                Instance = context.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? context.TraceIdentifier } }
            },
            _ => null
        };
        if (problem == null)
        {
            Log.Error("Found no problem response for status {Code}. No body will be added.", status);
            return;
        }

        await Results.Problem(problem).ExecuteAsync(context);
    }

    private static Dictionary<string, string[]> Get401ErrorMessages()
    {
        return new Dictionary<string, string[]>
        {
            ["Unauthorized"] = [Constants.SwaggerSummary.AuthenticationFailure]
        };
    }

    private static Dictionary<string, string[]> Get403ErrorMessages(HttpContext context)
    {
        var messages = new Dictionary<string, string[]>();
        var endpoint = context.GetEndpoint();
        if (endpoint == null) return messages;

        var aspNetMethodAttributes = endpoint.Metadata.OfType<OpenApiExtrasAttribute>();
        var fastEndpointsAttributes = endpoint.Metadata.OfType<EndpointDefinition>()
            .FirstOrDefault()?
            .EndpointAttributes?
            .OfType<OpenApiExtrasAttribute>() ?? [];

        var extras = aspNetMethodAttributes.Concat(fastEndpointsAttributes).FirstOrDefault();

        if (extras != null) messages["Forbidden"] = [AuthorizationFailureMessageBuilder.DefaultForbiddenFor(extras).Build()];
        var displayUrl = context.Request.PathBase + context.Request.Path;

        Log.Warning("Found no Endpoint metadata for request url {Url}.", displayUrl);
        return messages;
    }
}
