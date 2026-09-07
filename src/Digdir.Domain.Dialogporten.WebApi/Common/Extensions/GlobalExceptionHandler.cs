using Digdir.Domain.Dialogporten.Infrastructure.Common.Exceptions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Factory;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Rules;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            BadHttpRequestException badHttpRequestException => badHttpRequestException.StatusCode,
            IUpstreamServiceError => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/problem+json";
        var problemDetails = ProblemDetailsRules.TryCreateProblemDetails(ctx, []);

        if (statusCode >= 500 || problemDetails is null)
        {
            var http = $"{ctx.Request.Scheme}: {ctx.Request.Method} {ctx.Request.Path}";
            var type = exception.GetType().Name;
            var error = exception.Message;
            var logger = ctx.Resolve<ILogger<GlobalExceptionHandler>>();
            logger.LogError(exception, "{@Http} {@Type} {@Reason}", http, type, error);
        }

        problemDetails ??= ProblemDetailsBuilderFactory.Fallback(statusCode).ForContext(ctx).Build();

        await ctx.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
