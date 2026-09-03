using System.Diagnostics;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Problem;

internal sealed class ProblemDetailsBuilder
{
    private int _statusCode;
    private string _title = string.Empty;
    private string? _detail;
    private string _type = string.Empty;
    private Dictionary<string, string[]>? _errors;

    private ProblemDetailsBuilder()
    {
    }

    public static ProblemDetailsBuilder Unauthorized(Dictionary<string, string[]>? errors = null)
    {
        return new ProblemDetailsBuilder()
            .WithTitle("Unauthorized.")
            .WithStatusCode(StatusCodes.Status401Unauthorized)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2")
            .WithErrors(errors);
    }

    public static ProblemDetailsBuilder Forbidden(Dictionary<string, string[]>? errors = null)
    {
        return new ProblemDetailsBuilder()
            .WithTitle("Forbidden.")
            .WithStatusCode(StatusCodes.Status403Forbidden)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4")
            .WithErrors(errors);
    }

    public static ProblemDetailsBuilder NotFound(Dictionary<string, string[]>? errors = null)
    {
        return new ProblemDetailsBuilder()
            .WithTitle("Resource not found.")
            .WithStatusCode(StatusCodes.Status404NotFound)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5")
            .WithErrors(errors);
    }

    public static ProblemDetailsBuilder PayloadTooLarge()
    {
        return new ProblemDetailsBuilder()
            .WithTitle($"Payload too large. The maximum allowed size is {Constants.MaxRequestBodySizeInBytes} bytes.")
            .WithStatusCode(StatusCodes.Status413PayloadTooLarge)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.14");
    }

    public static ProblemDetailsBuilder BadRequest(Dictionary<string, string[]>? errors = null)
    {
        return new ProblemDetailsBuilder()
            .WithTitle("One or more validation errors occurred.")
            .WithStatusCode(StatusCodes.Status400BadRequest)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1")
            .WithErrors(errors);
    }

    public static ProblemDetailsBuilder NotAcceptable(Dictionary<string, string[]>? errors = null)
    {
        return new ProblemDetailsBuilder()
            .WithTitle("Requested content type is not acceptable.")
            .WithDetail("The Accept header must allow JSON responses.")
            .WithStatusCode(StatusCodes.Status406NotAcceptable)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.7")
            .WithErrors(errors);
    }

    public static ProblemDetailsBuilder Conflict(Dictionary<string, string[]>? errors = null)
    {
        return new ProblemDetailsBuilder()
            .WithTitle("Conflict.")
            .WithStatusCode(StatusCodes.Status409Conflict)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10")
            .WithErrors(errors);
    }

    public static ProblemDetailsBuilder Gone(Dictionary<string, string[]>? errors = null)
    {
        return new ProblemDetailsBuilder()
            .WithTitle("Resource no longer available.")
            .WithStatusCode(StatusCodes.Status410Gone)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.11")
            .WithErrors(errors);
    }

    public static ProblemDetailsBuilder PreconditionFailed()
    {
        return new ProblemDetailsBuilder()
            .WithTitle("Precondition failed.")
            .WithStatusCode(StatusCodes.Status412PreconditionFailed)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.13");
    }

    public static ProblemDetailsBuilder UnprocessableEntity(Dictionary<string, string[]>? errors = null)
    {
        return new ProblemDetailsBuilder()
            .WithTitle("Unprocessable request.")
            .WithStatusCode(StatusCodes.Status422UnprocessableEntity)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.21")
            .WithErrors(errors);
    }

    public static ProblemDetailsBuilder BadGateway()
    {
        return new ProblemDetailsBuilder()
            .WithTitle("Bad gateway.")
            .WithDetail("An upstream server is down or returned an invalid response. Please try again later.")
            .WithStatusCode(StatusCodes.Status502BadGateway)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.3");
    }

    public static ProblemDetailsBuilder Fallback(int statusCode)
    {
        return new ProblemDetailsBuilder()
            .WithTitle("An error occurred while processing the request.")
            .WithDetail("Something went wrong during the request.")
            .WithStatusCode(statusCode)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15");
    }

    public ProblemDetailsBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public ProblemDetailsBuilder WithStatusCode(int statusCode)
    {
        _statusCode = statusCode;
        return this;
    }

    public ProblemDetailsBuilder WithDetail(string detail)
    {
        _detail = detail;
        return this;
    }

    public ProblemDetailsBuilder WithType(string type)
    {
        _type = type;
        return this;
    }

    public ProblemDetailsBuilder WithErrors(Dictionary<string, string[]>? errors)
    {
        _errors = errors;
        return this;
    }

    public ProblemDetailsBuilder WithErrors(List<ValidationFailure> failures)
    {
        _errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(x => x.Key, x => x.Select(m => m.ErrorMessage).ToArray());
        return this;
    }

    public ProblemDetails Build(HttpContext forContext)
    {
        var problemDetails = _errors is not null
            ? new ValidationProblemDetails(_errors)
            : new ProblemDetails();

        problemDetails.Title = _title;
        problemDetails.Type = _type;
        problemDetails.Detail = _detail;
        problemDetails.Status = _statusCode;
        problemDetails.Instance = forContext.Request.Path;
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? forContext.TraceIdentifier;

        return problemDetails;
    }
}
