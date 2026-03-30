using System.Text.Json;
using Refit;
using ProblemDetails = Altinn.ApiClients.Dialogporten.Features.V1.ServiceOwner.ProblemDetails;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner;

internal static class ApiResponseExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Ensures the response is successful, throwing <see cref="DialogportenApiException"/> on failure.
    /// Returns the typed content on success.
    /// </summary>
    internal static T EnsureSuccess<T>(this IApiResponse<T> response) =>
        response.IsSuccessStatusCode
            ? response.Content!
            : throw CreateException(response);

    /// <summary>
    /// Ensures a content-less response is successful, throwing <see cref="DialogportenApiException"/> on failure.
    /// </summary>
    internal static void EnsureSuccess(this IApiResponse response)
    {
        if (response.IsSuccessStatusCode)
            return;

        throw CreateException(response);
    }

    private static DialogportenApiException CreateException(IApiResponse response)
    {
        var rawContent = response.Error?.Content;
        ProblemDetails? problemDetails = null;

        if (rawContent is not null)
        {
            try
            {
                problemDetails = JsonSerializer.Deserialize<ProblemDetails>(rawContent, JsonOptions);
            }
            catch (JsonException)
            {
                // Ignore deserialization failures — rawContent is still available
            }
        }

        return new DialogportenApiException(response.StatusCode, problemDetails, rawContent);
    }
}
