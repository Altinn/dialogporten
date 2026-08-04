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
        var result = await _sender.Send(new GetSearchTermsQuery
        {
            AcceptedLanguages = req.AcceptedLanguages?.AcceptedLanguage
        }, ct);

        await result.Match(
            async dto =>
            {
                // Strong validator derived from the generation timestamp; identical across all
                // languages of a single generation run.
                var etag = $"\"{dto.GeneratedAt.UtcTicks}\"";
                HttpContext.Response.Headers.ETag = etag;
                HttpContext.Response.Headers.LastModified =
                    dto.GeneratedAt.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);

                if (IsNotModified(etag, dto.GeneratedAt))
                {
                    await Send.ResultAsync(Results.StatusCode(StatusCodes.Status304NotModified));
                    return;
                }

                await Send.OkAsync(GetSearchTermsResponse.From(dto), ct);
            },
            notFound => this.NotFoundAsync(notFound, ct));
    }

    private bool IsNotModified(string etag, DateTimeOffset generatedAt)
    {
        var ifNoneMatch = HttpContext.Request.Headers.IfNoneMatch;
        if (ifNoneMatch.Count > 0)
        {
            return ifNoneMatch.Contains(etag) || ifNoneMatch.Contains("*");
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

[OpenApiTypeName(nameof(SearchTermResponseItem))]
public sealed class SearchTermResponseItem
{
    [JsonPropertyName("w")]
    public required string Word { get; init; }

    [JsonPropertyName("s")]
    public required IReadOnlyList<string> Resources { get; init; }
}
