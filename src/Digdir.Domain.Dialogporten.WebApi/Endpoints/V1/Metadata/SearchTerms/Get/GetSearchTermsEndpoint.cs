using System.Globalization;
using System.Text.Json.Serialization;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.SearchTerms.Queries.Get;
using Digdir.Domain.Dialogporten.WebApi.Common;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Extensions;
using Digdir.Library.Utils.AspNet;
using FastEndpoints;
using MediatR;
using Microsoft.Net.Http.Headers;
using Constants = Digdir.Domain.Dialogporten.WebApi.Common.Constants;

namespace Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Metadata.SearchTerms.Get;

[OpenApiOperationId("GetSearchTerms")]
public sealed class GetSearchTermsEndpoint : Endpoint<GetSearchTermsRequest, GetSearchTermsResponse>
{
    private readonly ISender _sender;

    public GetSearchTermsEndpoint(ISender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);
        _sender = sender;
    }

    public override void Configure()
    {
        Get("metadata/searchterms");
        Group<MetadataGroup>();

        Description(b => b.ProducesOneOf<GetSearchTermsResponse>(
            StatusCodes.Status200OK,
            StatusCodes.Status304NotModified,
            StatusCodes.Status404NotFound));
    }

    [EnableResponseCompression]
    public override async Task HandleAsync(GetSearchTermsRequest req, CancellationToken ct)
    {
        // The representation is negotiated on Accept-Language; without Vary, shared caches would
        // serve one language's payload to clients requesting another. Set before matching the
        // result so it also covers 404 — a language-specific not-found is cacheable too.
        HttpContext.Response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptLanguage);

        var acceptedLanguages = req.AcceptedLanguages?.AcceptedLanguage;

        // Conditional requests get a metadata-only round trip first: regeneration is nightly and
        // clients revalidate with ETags, so in steady state nearly every request is a 304 — which
        // must not pay for fetching and deserializing the full jsonb wordlist. Unconditional
        // requests need the full payload no matter what, so they skip straight to it.
        if (HasConditionalHeaders())
        {
            var metaResult = await _sender.Send(new GetSearchTermsQuery
            {
                AcceptedLanguages = acceptedLanguages,
                MetadataOnly = true
            }, ct);

            if (metaResult.TryPickT1(out var notFound, out var meta))
            {
                await this.NotFoundAsync(notFound, ct);
                return;
            }

            var etag = BuildEtag(meta);
            if (IsNotModified(etag, meta.GeneratedAt))
            {
                SetValidatorHeaders(etag, meta.GeneratedAt);
                await Send.ResultAsync(Results.StatusCode(StatusCodes.Status304NotModified));
                return;
            }
        }

        var result = await _sender.Send(new GetSearchTermsQuery
        {
            AcceptedLanguages = acceptedLanguages
        }, ct);

        await result.Match(
            async dto =>
            {
                // Validators derive from the payload actually served: a regeneration landing
                // between the two queries changes GeneratedAt, and the headers must match the body.
                SetValidatorHeaders(BuildEtag(dto), dto.GeneratedAt);
                await Send.OkAsync(GetSearchTermsResponse.From(dto), ct);
            },
            nf => this.NotFoundAsync(nf, ct));
    }

    // Strong validator derived from the resolved language and the generation timestamp.
    // The language MUST be part of the validator: the same URL serves different
    // representations per Accept-Language, and a timestamp-only ETag would let a client
    // switching languages revalidate its old copy into a bogus 304.
    private static string BuildEtag(SearchTermsDto dto) => $"\"{dto.Language}-{dto.GeneratedAt.UtcTicks}\"";

    private void SetValidatorHeaders(string etag, DateTimeOffset generatedAt)
    {
        HttpContext.Response.Headers.ETag = etag;
        HttpContext.Response.Headers.LastModified =
            generatedAt.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
    }

    private bool HasConditionalHeaders() =>
        HttpContext.Request.Headers.IfNoneMatch.Count > 0
        || !string.IsNullOrEmpty(HttpContext.Request.Headers.IfModifiedSince.ToString());

    private bool IsNotModified(string etag, DateTimeOffset generatedAt)
    {
        var ifNoneMatch = HttpContext.Request.Headers.IfNoneMatch;
        if (ifNoneMatch.Count > 0)
        {
            // Parse rather than string-compare whole header values: a single header line may
            // carry a comma-separated etag list, and intermediaries may weaken the tag (W/"...").
            // If-None-Match uses weak comparison per RFC 9110 §13.1.2.
            return EntityTagHeaderValue.TryParseList(ifNoneMatch, out var candidates)
                   && candidates.Any(candidate =>
                       candidate.Equals(EntityTagHeaderValue.Any)
                       || candidate.Compare(new EntityTagHeaderValue(etag), useStrongComparison: false));
        }

        var ifModifiedSince = HttpContext.Request.Headers.IfModifiedSince.ToString();
        if (!string.IsNullOrEmpty(ifModifiedSince)
            && DateTimeOffset.TryParse(ifModifiedSince, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var since))
        {
            // HTTP-date has 1-second resolution; truncate the generation timestamp before comparing.
            var generatedAtSeconds = generatedAt.AddTicks(-(generatedAt.UtcTicks % TimeSpan.TicksPerSecond));
            return generatedAtSeconds <= since;
        }

        return false;
    }
}

[OpenApiTypeName(nameof(GetSearchTermsRequest))]
public sealed class GetSearchTermsRequest
{
    [FromHeader(Constants.AcceptLanguage, isRequired: false)]
    public AcceptedLanguages? AcceptedLanguages { get; set; } = null;
}

[OpenApiTypeName(nameof(GetSearchTermsResponse))]
public sealed class GetSearchTermsResponse
{
    [JsonPropertyName("l")]
    public required string Language { get; init; }

    [JsonPropertyName("generatedAt")]
    public required DateTimeOffset GeneratedAt { get; init; }

    [JsonPropertyName("words")]
    public required IReadOnlyList<SearchTermResponseItem> Words { get; init; }

    public static GetSearchTermsResponse From(SearchTermsDto dto) => new()
    {
        Language = dto.Language,
        GeneratedAt = dto.GeneratedAt,
        Words = dto.Words
            .Select(w => new SearchTermResponseItem { Word = w.Word, Resources = w.Resources })
            .ToList()
    };
}

// Wire twin of Domain.SearchTerms.SearchTermEntry; kept as a distinct type so the public
// OpenAPI contract is owned by the endpoint. (Deliberately not an XML doc comment — that
// would leak into the OpenAPI schema description and churn the verified snapshots.)
[OpenApiTypeName(nameof(SearchTermResponseItem))]
public sealed class SearchTermResponseItem
{
    [JsonPropertyName("w")]
    public required string Word { get; init; }

    [JsonPropertyName("s")]
    public required IReadOnlyList<string> Resources { get; init; }
}
