using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Rules;
using Microsoft.AspNetCore.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public static class UseStatusCodePagesHandlers
{
    public static async Task CreateStatusCodePageProblemDetails(StatusCodeContext statusCodeContext)
    {
        var context = statusCodeContext.HttpContext;
        await context.Response.SendProblemDetailsAsync(
            [],
            context.Response.StatusCode,
            cancellation: statusCodeContext.HttpContext.RequestAborted
        );
    }
}
