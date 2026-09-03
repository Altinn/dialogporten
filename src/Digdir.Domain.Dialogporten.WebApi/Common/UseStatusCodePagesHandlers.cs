using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Microsoft.AspNetCore.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public static class UseStatusCodePagesHandlers
{
    public static async Task CreateStatusCodePageProblemDetails(StatusCodeContext statusCodeContext)
    {
        var context = statusCodeContext.HttpContext;
        var problem = statusCodeContext.HttpContext.CreateInfrastructureProblemDetailsOrDefault();

        await Results.Problem(problem).ExecuteAsync(context);
    }
}
