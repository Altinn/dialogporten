using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public static class UseStatusCodePagesHandlers
{
    public static async Task CreateStatusCodePageProblemDetails(StatusCodeContext statusCodeContext)
    {
        var context = statusCodeContext.HttpContext;
        await context.Response.SendErrorsAsync([], context.Response.StatusCode);
    }
}
