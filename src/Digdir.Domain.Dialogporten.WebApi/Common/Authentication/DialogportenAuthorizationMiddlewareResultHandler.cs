using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using FastEndpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using AuthorizationPolicy = Microsoft.AspNetCore.Authorization.AuthorizationPolicy;
using ContentType = Azure.Core.ContentType;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Authentication;

internal sealed class DialogportenAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly ILogger<DialogportenAuthorizationMiddlewareResultHandler> _logger;

    public DialogportenAuthorizationMiddlewareResultHandler(
        ILogger<DialogportenAuthorizationMiddlewareResultHandler> logger
    )
    {
        _logger = logger;
    }

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult
    )
    {
        if (authorizeResult.Forbidden)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = ContentType.ApplicationJson.ToString();

            var error = GetForbiddenMessageFromEndpointSummary(context) ?? "Unauthorized Access";
            await context.Response.WriteAsJsonAsync(new Forbidden(error).ToValidationResults(), context.RequestAborted);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private string? GetForbiddenMessageFromEndpointSummary(HttpContext context)
    {
        var endpointMetadata = context.GetEndpoint()?.Metadata;
        var displayUrl = context.Request.PathBase + context.Request.Path;
        if (endpointMetadata == null)
        {
            _logger.LogWarning("Found no Endpoint metadata for request url {Url}.", displayUrl);
            return null;
        }

        foreach (var entry in endpointMetadata)
        {
            if (entry is not EndpointDefinition endpointDefinition) continue;

            var endpointDefinitionEndpointSummary = endpointDefinition.EndpointSummary;
            var type = endpointDefinition.EndpointType;
            if (endpointDefinitionEndpointSummary == null)
            {
                _logger.LogWarning("Found no EndpointSummary for endpoint {Type}.", type);
                return null;
            }
            var message = endpointDefinitionEndpointSummary.Responses.GetValueOrDefault(StatusCodes.Status403Forbidden);

            if (message != null) return message;

            _logger.LogWarning("Found no 403 message in EndpointSummary for endpoint {Type}.", type);
            return null;
        }

        _logger.LogWarning("Found no EndpointDefinition for request url {Url}.", displayUrl);
        return null;
    }
}
