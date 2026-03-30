using System.Net;
using ProblemDetails = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.ProblemDetails;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

/// <summary>
/// Thrown when the Dialogporten API returns a non-success status code.
/// </summary>
public sealed class DialogportenApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ProblemDetails? ProblemDetails { get; }
    public string? RawContent { get; }

    public DialogportenApiException(HttpStatusCode statusCode, ProblemDetails? problemDetails, string? rawContent)
        : base(FormatMessage(statusCode, problemDetails, rawContent))
    {
        StatusCode = statusCode;
        ProblemDetails = problemDetails;
        RawContent = rawContent;
    }

    private static string FormatMessage(HttpStatusCode statusCode, ProblemDetails? problemDetails, string? rawContent)
    {
        if (problemDetails?.Title is not null)
            return $"Dialogporten API error {(int)statusCode}: {problemDetails.Title}";

        return $"Dialogporten API error {(int)statusCode}: {rawContent ?? statusCode.ToString()}";
    }
}
