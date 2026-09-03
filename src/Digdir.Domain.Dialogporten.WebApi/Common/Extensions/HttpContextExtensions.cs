using Digdir.Domain.Dialogporten.WebApi.Common.Problem;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using EndpointDefinition = FastEndpoints.EndpointDefinition;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

internal static class HttpContextExtensions
{
    extension(HttpContext ctx)
    {
        /// <summary>
        /// This method creates a ProblemDetails from the ASP HttpContext.
        /// Here we handle the cases where FastEndpoints can't build a ProblemDetails object.
        /// This happens when ASP writes the response before FastEndpoints middleware runs, typically for cases like:
        /// - Routing
        /// - Authentication
        /// - Authorization (policy violations)
        /// </summary>
        /// <returns></returns>
        public ProblemDetails CreateInfrastructureProblemDetailsOrDefault()
        {
            var statusCode = ctx.Response.StatusCode;

            var builder = statusCode switch
            {
                StatusCodes.Status401Unauthorized => ProblemDetailsBuilder.Unauthorized(Get401ErrorMessages()),
                StatusCodes.Status403Forbidden => ProblemDetailsBuilder.Forbidden(Get403ErrorMessages(ctx)),
                StatusCodes.Status404NotFound => ProblemDetailsBuilder.NotFound().WithTitle("Endpoint not found."),
                _ => CreateFallbackProblemDetails(statusCode, "Infrastructure")
            };
            return builder.Build(ctx);
        }

        /// <summary>
        /// This method creates a ProblemDetails from the ASP HttpContext, after it has been modified by FastEndpoints.
        /// If the endpoint wrote a ValidationFailures object, these are included as Errors.
        /// The endpoint typically writes a ValidationFalures object when the application layer has returned an error.
        /// </summary>
        /// <returns></returns>
        public ProblemDetails CreateApplicationProblemDetailsOrDefault(List<ValidationFailure> failures)
        {
            return ctx.TryCreateApplicationProblemDetails(failures) ??
                   CreateFallbackProblemDetails(ctx.Response.StatusCode, "Application").Build(ctx);
        }

        /// <summary>
        /// Same as <see cref="CreateApplicationProblemDetailsOrDefault"/>
        /// </summary>
        /// <returns></returns>
        public ProblemDetails? TryCreateApplicationProblemDetails(List<ValidationFailure> failures)
        {
            var errors = failures
                .GroupBy(f => f.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(m => m.ErrorMessage).ToArray());
            var builder = ctx.Response.StatusCode switch
            {
                StatusCodes.Status413PayloadTooLarge => ProblemDetailsBuilder.PayloadTooLarge(),
                StatusCodes.Status400BadRequest => ProblemDetailsBuilder.BadRequest(errors),
                StatusCodes.Status403Forbidden => ProblemDetailsBuilder.Forbidden(errors),
                StatusCodes.Status404NotFound => ProblemDetailsBuilder.NotFound(errors),
                StatusCodes.Status406NotAcceptable => ProblemDetailsBuilder.NotAcceptable(errors),
                StatusCodes.Status409Conflict => ProblemDetailsBuilder.Conflict(errors),
                StatusCodes.Status410Gone => ProblemDetailsBuilder.Gone(errors),
                StatusCodes.Status412PreconditionFailed => ProblemDetailsBuilder.PreconditionFailed(),
                StatusCodes.Status422UnprocessableEntity => ProblemDetailsBuilder.UnprocessableEntity(errors),
                StatusCodes.Status502BadGateway => ProblemDetailsBuilder.BadGateway(),
                _ => null
            };
            return builder?.Build(ctx);
        }
    }

    private static ProblemDetailsBuilder CreateFallbackProblemDetails(int statusCode, string source)
    {
        Log.Error("No ProblemDetails was mapped for {StatusCode} from {ProblemSource}", statusCode, source);

        return ProblemDetailsBuilder.Fallback(statusCode);
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

        if (extras != null)
        {
            messages["Forbidden"] = [AuthorizationFailureMessageBuilder.DefaultForbiddenFor(extras).Build()];
        }

        var displayUrl = context.Request.PathBase + context.Request.Path;

        Log.Warning("Found no Endpoint metadata for request url {Url}.", displayUrl);
        return messages;
    }
}
