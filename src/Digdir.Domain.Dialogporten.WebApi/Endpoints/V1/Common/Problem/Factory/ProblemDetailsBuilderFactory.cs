using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Builder;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Types;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Factory;

public class ProblemDetailsBuilderFactory
{

    public static ProblemDetailsBuilder<ProblemDetails> Unauthorized()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("Unauthorized.")
            .WithStatusCode(StatusCodes.Status401Unauthorized)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.2");
    }

    public static ProblemDetailsBuilder<ProblemDetails> Forbidden()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("Forbidden.")
            .WithStatusCode(StatusCodes.Status403Forbidden)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.4");
    }

    public static ProblemDetailsBuilder<ProblemDetails> NotFound()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("Resource not found.")
            .WithStatusCode(StatusCodes.Status404NotFound)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.5");
    }

    public static ProblemDetailsBuilder<ProblemDetails> PayloadTooLarge()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle($"Payload too large. The maximum allowed size is {Constants.MaxRequestBodySizeInBytes} bytes.")
            .WithStatusCode(StatusCodes.Status413PayloadTooLarge)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.14");
    }

    public static ProblemDetailsBuilder<ProblemDetails> BadRequest()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("One or more validation errors occurred.")
            .WithStatusCode(StatusCodes.Status400BadRequest)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.1");
    }

    public static ProblemDetailsBuilder<ProblemDetails> NotAcceptable()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("Requested content type is not acceptable.")
            .WithDetail("The Accept header must allow JSON responses.")
            .WithStatusCode(StatusCodes.Status406NotAcceptable)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.7");
    }

    public static ProblemDetailsBuilder<ProblemDetails> Conflict()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("Conflict.")
            .WithStatusCode(StatusCodes.Status409Conflict)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.10");
    }

    public static ProblemDetailsBuilder<ProblemDetails> Gone()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("Resource no longer available.")
            .WithStatusCode(StatusCodes.Status410Gone)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.11");
    }

    public static ProblemDetailsBuilder<ProblemDetails> PreconditionFailed()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("Precondition failed.")
            .WithStatusCode(StatusCodes.Status412PreconditionFailed)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.13");
    }

    public static ProblemDetailsBuilder<ProblemDetails> UnprocessableEntity()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("Unprocessable request.")
            .WithStatusCode(StatusCodes.Status422UnprocessableEntity)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.5.21");
    }

    public static ProblemDetailsBuilder<ProblemDetails> BadGateway()
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("Bad gateway.")
            .WithDetail("An upstream server is down or returned an invalid response. Please try again later.")
            .WithStatusCode(StatusCodes.Status502BadGateway)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15.6.3");
    }

    public static ProblemDetailsBuilder<ProblemDetails> Fallback(int statusCode)
    {
        return new ProblemDetailsBuilder<ProblemDetails>(new ProblemDetails())
            .WithTitle("An error occurred while processing the request.")
            .WithDetail("Something went wrong during the request.")
            .WithStatusCode(statusCode)
            .WithType("https://datatracker.ietf.org/doc/html/rfc9110#section-15");
    }
}
