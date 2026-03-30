using Refit;

namespace Altinn.ApiClients.Dialogporten.ServiceOwner.V1;

internal static class ApiResponseMappingExtensions
{
    public static IApiResponse<TTarget> MapContent<TSource, TTarget>(
        this IApiResponse<TSource> response,
        Func<TSource, TTarget> map)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(map);

        var httpResponse = new HttpResponseMessage(response.StatusCode)
        {
            ReasonPhrase = response.ReasonPhrase,
            RequestMessage = response.RequestMessage,
            Version = response.Version
        };

        foreach (var header in response.Headers)
        {
            httpResponse.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (response.ContentHeaders is not null && response.ContentHeaders.Any())
        {
            httpResponse.Content = new ByteArrayContent([]);

            foreach (var contentHeader in response.ContentHeaders)
            {
                httpResponse.Content.Headers.TryAddWithoutValidation(contentHeader.Key, contentHeader.Value);
            }
        }

        var settings = response is ApiResponse<TSource> apiResponse
            ? apiResponse.Settings
            : new RefitSettings();

        var content = response.Content is null ? default! : map(response.Content);
        return new ApiResponse<TTarget>(httpResponse, content, settings, response.Error);
    }
}
