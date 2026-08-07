using System.Text.Json;
using System.Text.Json.Serialization;
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

        var document = await _db.SearchTermLists
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Language == language, cancellationToken);

        if (document is null)
        {
            return new EntityNotFound<SearchTermList>([language]);
        }

        var words = JsonSerializer.Deserialize<List<StoredSearchTerm>>(document.Words) ?? [];

        return new SearchTermsDto
        {
            Language = document.Language,
            GeneratedAt = document.GeneratedAt,
            Words = words
                .Select(w => new SearchTermDto { Word = w.Word, Resources = w.Resources })
                .ToList()
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

    // Mirrors the terse jsonb storage shape: { "w": word, "s": [resources] }.
    private sealed record StoredSearchTerm(
        [property: JsonPropertyName("w")] string Word,
        [property: JsonPropertyName("s")] IReadOnlyList<string> Resources);
}
