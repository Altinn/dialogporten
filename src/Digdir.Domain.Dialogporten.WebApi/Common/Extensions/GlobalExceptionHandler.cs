using Digdir.Domain.Dialogporten.Infrastructure.Common.Exceptions;
using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception exception,
        CancellationToken cancellationToken)
    {
        ctx.Response.StatusCode = exception switch
        {
            BadHttpRequestException badHttpRequestException => badHttpRequestException.StatusCode,
            IUpstreamServiceError => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        ctx.Response.ContentType = "application/problem+json";
        var response = ctx.ResponseBuilder();

        if (ctx.Response.StatusCode >= 500 || response is null)
        {
            var http = $"{ctx.Request.Scheme}: {ctx.Request.Method} {ctx.Request.Path}";
            var type = exception.GetType().Name;
            var error = exception.Message;
            var logger = ctx.Resolve<ILogger<GlobalExceptionHandler>>();
            logger.LogError(exception, "{@Http} {@Type} {@Reason}", http, type, error);
        }

        await ctx.Response.WriteAsJsonAsync(response ?? ctx.DefaultResponse(), cancellationToken);
        return true;
    }
}
