using System.Text.Json;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Domain.SearchTerms;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.SearchTerms.Queries.Get;

public sealed class GetSearchTermsQuery : IRequest<GetSearchTermsResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public List<AcceptedLanguage>? AcceptedLanguages { get; set; }

    /// <summary>
    /// When true, only <see cref="SearchTermsDto.Language"/> and <see cref="SearchTermsDto.GeneratedAt"/>
    /// are populated (<see cref="SearchTermsDto.Words"/> is empty), skipping the jsonb wordlist transfer
    /// and deserialization. Lets HTTP conditional requests (ETag / If-Modified-Since) be answered
    /// without paying for the payload they won't return.
    /// </summary>
    public bool MetadataOnly { get; set; }
}

[GenerateOneOf]
public sealed partial class GetSearchTermsResult : OneOfBase<SearchTermsDto, EntityNotFound>;

internal sealed class GetSearchTermsQueryHandler : IRequestHandler<GetSearchTermsQuery, GetSearchTermsResult>
{
    private const string DefaultLanguage = "nb";
    private static readonly string[] SupportedLanguages = ["nb", "nn", "en"];

    private readonly IDialogDbContext _db;

    public GetSearchTermsQueryHandler(IDialogDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _db = db;
    }

    public async Task<GetSearchTermsResult> Handle(GetSearchTermsQuery request, CancellationToken cancellationToken)
    {
        var language = ResolveLanguage(request.AcceptedLanguages);

        if (request.MetadataOnly)
        {
            // Project past the jsonb Words column entirely — this is the hot path for 304s.
            var meta = await _db.SearchTermLists
                .AsNoTracking()
                .Where(x => x.Language == language)
                .Select(x => new { x.GeneratedAt })
                .FirstOrDefaultAsync(cancellationToken);

            return meta is null
                ? new EntityNotFound<SearchTermList>([language])
                : new SearchTermsDto
                {
                    Language = language,
                    GeneratedAt = meta.GeneratedAt,
                    Words = []
                };
        }

        var document = await _db.SearchTermLists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Language == language, cancellationToken);

        if (document is null)
        {
            return new EntityNotFound<SearchTermList>([language]);
        }

        // The jsonb column stores the shared terse contract (SearchTermEntry) verbatim.
        var words = JsonSerializer.Deserialize<List<SearchTermEntry>>(document.Words) ?? [];

        return new SearchTermsDto
        {
            Language = document.Language,
            GeneratedAt = document.GeneratedAt,
            Words = words
        };
    }

    /// <summary>
    /// Picks the highest-weighted requested language that we actually publish, falling back to
    /// <see cref="DefaultLanguage"/> (nb) when the client requested nothing supported. Language codes
    /// in <see cref="AcceptedLanguage"/> are already normalized to two-letter form.
    /// </summary>
    private static string ResolveLanguage(List<AcceptedLanguage>? acceptedLanguages)
    {
        if (acceptedLanguages is null || acceptedLanguages.Count == 0)
        {
            return DefaultLanguage;
        }

        return acceptedLanguages
            .Where(x => SupportedLanguages.Contains(x.LanguageCode, StringComparer.Ordinal))
            .OrderByDescending(x => x.Weight)
            .Select(x => x.LanguageCode)
            .FirstOrDefault() ?? DefaultLanguage;
    }
}
