﻿using FastEndpoints;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi;

internal static class ExceptionHandlerExtensions
{
    public static IApplicationBuilder UseProblemDetailsExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errApp =>
        {
            errApp.Run(async ctx =>
            {
                var exHandlerFeature = ctx.Features.Get<IExceptionHandlerFeature>();
                if (exHandlerFeature is null)
                {
                    return;
                }

                var http = exHandlerFeature.Endpoint?.DisplayName?.Split(" => ")[0];
                var type = exHandlerFeature.Error.GetType().Name;
                var error = exHandlerFeature.Error.Message;
                ctx.Resolve<ILogger<ExceptionHandler>>()
                    .LogError("{@http}{@type}{@reason}{@exception}", http, type, error, exHandlerFeature.Error);
                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                ctx.Response.ContentType = "application/problem+json";
                await ctx.Response.WriteAsJsonAsync(ctx.ResponseBuilder(ctx.Response.StatusCode));
            });
        });

        return app;
    }

    private class ExceptionHandler { }
}