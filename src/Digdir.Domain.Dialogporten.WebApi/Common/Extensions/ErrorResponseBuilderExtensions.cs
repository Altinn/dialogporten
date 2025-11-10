using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

internal static class ErrorResponseBuilderExtensions
{
    public static ProblemDetails DefaultResponse(this HttpContext ctx, int? statusCode = null) => new()
    {
        Title = "An error occurred while processing the request.",
        Detail = "Something went wrong during the request.",
        Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
        Status = statusCode ?? ctx.Response.StatusCode,
        Instance = ctx.Request.Path,
        Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
    };

    public static ProblemDetails GetResponseOrDefault(this HttpContext ctx, int statusCode,
        List<ValidationFailure>? failures = null) =>
        ctx.ResponseBuilder(failures, statusCode) ?? ctx.DefaultResponse(statusCode);

    public static object ResponseBuilder(List<ValidationFailure> failures, HttpContext ctx, int statusCode)
        => ctx.ResponseBuilder(failures, statusCode) ?? ctx.DefaultResponse(statusCode);

    public static ProblemDetails? ResponseBuilder(this HttpContext ctx, List<ValidationFailure>? failures = null, int? statusCode = null)
    {
        var errors = failures?
            .GroupBy(f => f.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(m => m.ErrorMessage).ToArray())
            ?? [];

        statusCode ??= ctx.Response.StatusCode;
        return statusCode switch
        {
            StatusCodes.Status413PayloadTooLarge => new ProblemDetails
            {
                Title = $"Payload too large. The maximum allowed size is {Constants.MaxRequestBodySizeInBytes} bytes.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.11",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
            },
            StatusCodes.Status400BadRequest => new ValidationProblemDetails(errors)
            {
                Title = "One or more validation errors occurred.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
            },
            StatusCodes.Status403Forbidden => new ValidationProblemDetails(errors)
            {
                Title = "Forbidden.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
            },
            StatusCodes.Status404NotFound => new ValidationProblemDetails(errors)
            {
                Title = "Resource not found.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
            },
            StatusCodes.Status409Conflict => new ValidationProblemDetails(errors)
            {
                Title = "Conflict.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
            },
            StatusCodes.Status410Gone => new ValidationProblemDetails(errors)
            {
                Title = "Resource no longer available.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.9",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
            },
            StatusCodes.Status412PreconditionFailed => new ProblemDetails
            {
                Title = "Precondition failed.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7232#section-4.2",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
            },
            StatusCodes.Status422UnprocessableEntity => new ValidationProblemDetails(errors)
            {
                Title = "Unprocessable request.",
                Type = "https://datatracker.ietf.org/doc/html/rfc4918#section-11.2",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
            },
            StatusCodes.Status502BadGateway => new ProblemDetails
            {
                Title = "Bad gateway.",
                Detail = "An upstream server is down or returned an invalid response. Please try again later.",
                Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.3",
                Status = statusCode,
                Instance = ctx.Request.Path,
                Extensions = { { "traceId", Activity.Current?.Id ?? ctx.TraceIdentifier } }
            },
            _ => null
        };
    }
}
