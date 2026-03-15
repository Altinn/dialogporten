using FluentValidation.Results;
using System.Diagnostics;
using ProblemDetails = FastEndpoints.ProblemDetails;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

internal static class ErrorResponseBuilderExtensions
{
    internal static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "One or more validation errors occurred.",
        StatusCodes.Status403Forbidden => "Forbidden.",
        StatusCodes.Status404NotFound => "Resource not found.",
        StatusCodes.Status406NotAcceptable => "Requested content type is not acceptable.",
        StatusCodes.Status409Conflict => "Conflict.",
        StatusCodes.Status410Gone => "Resource no longer available.",
        StatusCodes.Status412PreconditionFailed => "Precondition failed.",
        StatusCodes.Status413PayloadTooLarge => $"Payload too large. The maximum allowed size is {Constants.MaxRequestBodySizeInBytes} bytes.",
        StatusCodes.Status422UnprocessableEntity => "Unprocessable request.",
        StatusCodes.Status502BadGateway => "Bad gateway.",
        _ => "An error occurred while processing the request."
    };

    internal static string GetType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
        StatusCodes.Status403Forbidden => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
        StatusCodes.Status404NotFound => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
        StatusCodes.Status406NotAcceptable => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.6",
        StatusCodes.Status409Conflict => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8",
        StatusCodes.Status410Gone => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.9",
        StatusCodes.Status412PreconditionFailed => "https://datatracker.ietf.org/doc/html/rfc7232#section-4.2",
        StatusCodes.Status413PayloadTooLarge => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.11",
        StatusCodes.Status422UnprocessableEntity => "https://datatracker.ietf.org/doc/html/rfc4918#section-11.2",
        StatusCodes.Status502BadGateway => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.3",
        _ => "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
    };

    private static string? GetDetail(int statusCode) => statusCode switch
    {
        StatusCodes.Status406NotAcceptable => "The Accept header must allow JSON responses.",
        StatusCodes.Status502BadGateway => "An upstream server is down or returned an invalid response. Please try again later.",
        _ => null
    };

    extension(HttpContext ctx)
    {
        public ProblemDetails DefaultResponse(int? statusCode = null)
        {
            var code = statusCode ?? ctx.Response.StatusCode;
            return new ProblemDetails([], code)
            {
                Detail = "Something went wrong during the request.",
                Instance = ctx.Request.Path,
                TraceId = Activity.Current?.Id ?? ctx.TraceIdentifier
            };
        }

        public ProblemDetails GetResponseOrDefault(int statusCode,
            List<ValidationFailure>? failures = null) =>
            ctx.BuildProblemDetails(failures, statusCode) ?? ctx.DefaultResponse(statusCode);

        public ProblemDetails? BuildProblemDetails(List<ValidationFailure>? failures = null, int? statusCode = null)
        {
            statusCode ??= ctx.Response.StatusCode;

            if (ErrorResponseBuilderExtensions.GetType(statusCode.Value) == "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
                && statusCode != StatusCodes.Status500InternalServerError)
            {
                return null;
            }

            var detail = GetDetail(statusCode.Value);
            var pd = new ProblemDetails(failures ?? [], statusCode.Value)
            {
                Instance = ctx.Request.Path,
                TraceId = Activity.Current?.Id ?? ctx.TraceIdentifier
            };

            if (detail is not null)
            {
                pd.Detail = detail;
            }

            return pd;
        }
    }

    public static object ResponseBuilder(List<ValidationFailure> failures, HttpContext ctx, int statusCode)
        => ctx.BuildProblemDetails(failures, statusCode) ?? ctx.DefaultResponse(statusCode);
}
