using System.Diagnostics;
using Digdir.Domain.Dialogporten.WebApi.Common.Errors;
using FluentValidation.Results;
using Microsoft.AspNetCore.WebUtilities;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Extensions;

internal static class ErrorResponseBuilderExtensions
{
    public static DialogportenProblemDetails DefaultResponse(this HttpContext ctx, int? statusCode = null) =>
        Build(ctx,
            statusCode ?? ctx.Response.StatusCode,
            ErrorCodes.InternalServerError,
            "An error occurred while processing the request.",
            detail: "Something went wrong during the request.");

    public static DialogportenProblemDetails GetResponseOrDefault(this HttpContext ctx, int statusCode,
        List<ValidationFailure>? failures = null) =>
        ctx.ResponseBuilder(failures, statusCode) ?? ctx.DefaultResponse(statusCode);

    public static object ResponseBuilder(List<ValidationFailure> failures, HttpContext ctx, int statusCode)
        => ctx.ResponseBuilder(failures, statusCode) ?? ctx.DefaultResponse(statusCode);

    public static DialogportenProblemDetails? ResponseBuilder(this HttpContext ctx,
        List<ValidationFailure>? failures = null, int? statusCode = null)
    {
        statusCode ??= ctx.Response.StatusCode;
        var errors = ToErrorDictionary(failures);

        return statusCode switch
        {
            StatusCodes.Status400BadRequest => Build(ctx, statusCode.Value, ErrorCodes.Validation,
                "One or more validation errors occurred.",
                errors: errors ?? [],
                validationErrors: ToValidationErrors(failures)),
            StatusCodes.Status403Forbidden => Build(ctx, statusCode.Value, ErrorCodes.Forbidden,
                "Forbidden.", errors: errors ?? []),
            StatusCodes.Status404NotFound => Build(ctx, statusCode.Value, ErrorCodes.NotFound,
                "Resource not found.", errors: errors ?? []),
            StatusCodes.Status406NotAcceptable => Build(ctx, statusCode.Value, ErrorCodes.NotAcceptable,
                "Requested content type is not acceptable.",
                detail: "The Accept header must allow JSON responses.",
                errors: errors ?? []),
            StatusCodes.Status409Conflict => Build(ctx, statusCode.Value, ErrorCodes.Conflict,
                "Conflict.", errors: errors ?? []),
            StatusCodes.Status410Gone => Build(ctx, statusCode.Value, ErrorCodes.Gone,
                "Resource no longer available.", errors: errors ?? []),
            StatusCodes.Status412PreconditionFailed => Build(ctx, statusCode.Value, ErrorCodes.PreconditionFailed,
                "Precondition failed."),
            StatusCodes.Status413PayloadTooLarge => Build(ctx, statusCode.Value, ErrorCodes.PayloadTooLarge,
                $"Payload too large. The maximum allowed size is {Constants.MaxRequestBodySizeInBytes} bytes."),
            StatusCodes.Status422UnprocessableEntity => Build(ctx, statusCode.Value, ErrorCodes.UnprocessableEntity,
                "Unprocessable request.", errors: errors ?? []),
            StatusCodes.Status502BadGateway => Build(ctx, statusCode.Value, ErrorCodes.BadGateway,
                "Bad gateway.",
                detail: "An upstream server is down or returned an invalid response. Please try again later."),
            _ => null
        };
    }

    private static DialogportenProblemDetails Build(
        HttpContext ctx,
        int statusCode,
        string code,
        string title,
        string? detail = null,
        Dictionary<string, string[]>? errors = null,
        IReadOnlyList<ValidationErrorItem>? validationErrors = null,
        IReadOnlyList<DialogportenProblemDetails>? problems = null)
    {
        var result = new DialogportenProblemDetails
        {
            Type = ErrorCodes.ToUrn(code),
            Title = title,
            Status = statusCode,
            Instance = ctx.Request.Path,
            Code = code,
            StatusDescription = ReasonPhrases.GetReasonPhrase(statusCode),
            Errors = errors,
            ValidationErrors = validationErrors,
            Problems = problems
        };

        if (detail is not null)
        {
            result.Detail = detail;
        }

        result.TraceId = Activity.Current?.Id ?? ctx.TraceIdentifier;
        return result;
    }

    private static Dictionary<string, string[]>? ToErrorDictionary(List<ValidationFailure>? failures)
    {
        if (failures is null || failures.Count == 0)
        {
            return null;
        }

        return failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(m => m.ErrorMessage).ToArray());
    }

    private static List<ValidationErrorItem>? ToValidationErrors(List<ValidationFailure>? failures)
    {
        if (failures is null || failures.Count == 0)
        {
            return null;
        }

        return failures
            .Select(f => new ValidationErrorItem(ErrorCodes.ValidationError, f.ErrorMessage, [ToJsonPointer(f.PropertyName)]))
            .ToList();
    }

    private static string ToJsonPointer(string propertyName)
    {
        var path = propertyName.StartsWith("dto.", StringComparison.OrdinalIgnoreCase)
            ? propertyName[4..]
            : propertyName;

        var pointer = string.Join('/', path
            .Replace("]", "", StringComparison.Ordinal)
            .Split(['.', '['], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal)));

        return pointer.Length == 0 ? "" : $"/{pointer}";
    }
}
