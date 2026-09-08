using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Swagger;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Builder;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;
using FastEndpoints;
using FluentValidation.Results;
using ProblemDetailsBuilderFactory =
    Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Factory.ProblemDetailsBuilderFactory;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Rules;

public sealed class ProblemDetailsRules
{
    public static IDialogportenProblemDetails CreateProblemDetailsOrDefault(
        HttpContext ctx,
        List<ValidationFailure> failures
    )
    {
        return TryCreateProblemDetails(ctx, failures) ?? ProblemDetailsBuilderFactory
            .Fallback(ctx.Response.StatusCode)
            .ForContext(ctx)
            .Build();
    }

    public static IDialogportenProblemDetails? TryCreateProblemDetails(
        HttpContext ctx,
        List<ValidationFailure> failures
    )
    {
        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(m => m.ErrorMessage).ToArray());

        IProblemDetailsBuilder? builder = ctx.Response.StatusCode switch
        {
            StatusCodes.Status400BadRequest => ProblemDetailsBuilderFactory
                .BadRequest()
                .WithErrors(errors),
            StatusCodes.Status401Unauthorized => ProblemDetailsBuilderFactory
                .Unauthorized()
                .WithErrors(GetDefault401ErrorMessages()),
            StatusCodes.Status403Forbidden => ProblemDetailsBuilderFactory
                .Forbidden()
                .WithErrors(errors.Count == 0 ? GetDefault403ErrorMessages(ctx) : errors),
            StatusCodes.Status404NotFound => ProblemDetailsBuilderFactory
                .NotFound()
                .WithTitle(ctx.GetEndpoint() is null ? "Endpoint not found." : "Resource not found.")
                .WithErrors(errors),
            StatusCodes.Status406NotAcceptable => ProblemDetailsBuilderFactory
                .NotAcceptable()
                .WithErrors(errors),
            StatusCodes.Status409Conflict => ProblemDetailsBuilderFactory
                .Conflict()
                .WithErrors(errors),
            StatusCodes.Status410Gone => ProblemDetailsBuilderFactory
                .Gone()
                .WithErrors(errors),
            StatusCodes.Status412PreconditionFailed => ProblemDetailsBuilderFactory
                .PreconditionFailed()
                .WithErrors(errors),
            StatusCodes.Status413PayloadTooLarge => ProblemDetailsBuilderFactory
                .PayloadTooLarge()
                .WithErrors(errors),
            StatusCodes.Status422UnprocessableEntity => ProblemDetailsBuilderFactory
                .UnprocessableEntity()
                .WithErrors(errors),
            StatusCodes.Status502BadGateway => ProblemDetailsBuilderFactory
                .BadGateway()
                .WithErrors(errors),
            _ => null
        };

        return builder?.ForContext(ctx).Build();
    }

    private static Dictionary<string, string[]> GetDefault401ErrorMessages()
    {
        return new Dictionary<string, string[]>
        {
            ["Unauthorized"] = [Constants.SwaggerSummary.AuthenticationFailure]
        };
    }

    private static Dictionary<string, string[]> GetDefault403ErrorMessages(HttpContext context)
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
            return messages;
        }

        var displayUrl = context.Request.PathBase + context.Request.Path;

        var logger = context.Resolve<ILogger<ProblemDetailsRules>>();
        logger.LogError("Found no Endpoint metadata for request url {Url}.", displayUrl);
        return messages;
    }
}

public static class HttpResponseExtensions
{
    extension(HttpResponse response)
    {
        /// <summary>
        /// Convenience method to make a link between the SendErrorsAsync call and <see cref="ProblemDetailsRules"/>.
        /// </summary>
        public Task SendProblemDetailsAsync(List<ValidationFailure> failures,
            int statusCode = 400,
            CancellationToken cancellation = default)
        {
            return response.SendErrorsAsync(failures, statusCode, cancellation: cancellation);
        }
    }
}
