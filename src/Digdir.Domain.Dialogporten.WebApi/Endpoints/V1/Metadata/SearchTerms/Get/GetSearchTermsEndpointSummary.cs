using FastEndpoints;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Metadata.SearchTerms.Get;

public sealed class GetSearchTermsEndpointSummary : Summary<GetSearchTermsEndpoint>
{
    public GetSearchTermsEndpointSummary()
    {
        Summary = "Gets the curated search-term list used for client-side autocomplete.";
        Description = """
            Returns a curated list of search terms derived from dialog content, each linked to the
            service resources the term appears in. A single language is returned per request: `nb`
            (Bokmål) by default, or `nn`/`en` when requested via the `Accept-Language` header.

            The response includes the generation timestamp and supports conditional requests
            (`If-None-Match` / `If-Modified-Since`), returning `304 Not Modified` when the client's
            cached copy is current.
            """;
        Responses[StatusCodes.Status200OK] = "The search-term list for the resolved language.";
        Responses[StatusCodes.Status304NotModified] = "The cached search-term list is still current.";
        Responses[StatusCodes.Status404NotFound] = "No search-term list has been generated yet.";
        ResponseHeaders =
        [
            .. CacheValidatorHeaders(StatusCodes.Status200OK),
            .. CacheValidatorHeaders(StatusCodes.Status304NotModified)
        ];
    }

    private static ResponseHeader[] CacheValidatorHeaders(int statusCode) =>
    [
        new(statusCode, "ETag")
        {
            Description = "Strong validator for the returned representation; echo it in `If-None-Match` to revalidate.",
            Example = "\"nb-638849952000000000\""
        },
        new(statusCode, "Last-Modified")
        {
            Description = "Generation timestamp of the search-term list; usable with `If-Modified-Since`.",
            Example = "Tue, 16 Jun 2026 08:15:15 GMT"
        },
        new(statusCode, "Vary")
        {
            Description = "The representation is negotiated on `Accept-Language`.",
            Example = "Accept-Language"
        }
    ];
}
