using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.QueryDsl;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;

public sealed class ElasticRequest : IRequest<ElasticResult>
{
    public List<string> Party { get; set; } = [];
    public List<string> ServiceResource { get; set; } = [];
}

[GenerateOneOf]
public sealed partial class ElasticResult : OneOfBase<double>;

internal sealed class ElasticThingyHandler : IRequestHandler<ElasticRequest, ElasticResult>
{
    // private readonly IDialogDbContext _db;
    // private readonly IMapper _mapper;
    // private readonly IClock _clock;
    // private readonly IUserRegistry _userRegistry;
    private readonly IAltinnAuthorization _altinnAuthorization;

    public ElasticThingyHandler(
        // IDialogDbContext db,
        // IMapper mapper,
        // IClock clock,
        // IUserRegistry userRegistry,
        IAltinnAuthorization altinnAuthorization)
    {
        // _db = db ?? throw new ArgumentNullException(nameof(db));
        // _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        // _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        // _userRegistry = userRegistry ?? throw new ArgumentNullException(nameof(userRegistry));
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
    }

    public async Task<ElasticResult> Handle(ElasticRequest request, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();
        // var searchExpression = Expressions.LocalizedSearchExpression(request.Search, request.SearchLanguageCode);
        var authorizedResources = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
            request.Party,
            request.ServiceResource,
            cancellationToken: cancellationToken);

        var partyServiceResources = authorizedResources.ResourcesByParties
            .SelectMany(kvp => kvp.Value.Select(serviceResource => $"{kvp.Key}{serviceResource}"))
            .ToList();
        //
        // var mustPreFilters = new List<Func<QueryDescriptor<ElasticDialog>>>();
        //
        // foreach (var partyServiceResource in partyServiceResources)
        // {
        //     var partyServiceResourceId = partyServiceResource;
        //     mustPreFilters.Add(() => new QueryDescriptor<ElasticDialog>()
        //         .Match(m => m
        //             .Field(f => f.PartyServiceResourceId)
        //             .Query(partyServiceResourceId)));
        // }

        var mustPreFilters = partyServiceResources
            .Select(partyServiceResource => (Action<QueryDescriptor<ElasticDialog>>)(q => q
                .Match(m => m
                    .Field(f => f.PartyServiceResourceId)
                    .Query(partyServiceResource))))
            .ToArray();

        var elasticClient = new ElasticsearchClient();

        var response = await elasticClient
            .SearchAsync<ElasticDialog>(search => search
                .Index(nameof(ElasticDialog).ToLowerInvariant())
                .Size(0)
                .Query(query => query
                    .Bool(b => b.Should(mustPreFilters))
                )
                .Aggregations(ags => ags
                    .Add("agg_party_serviceresource",
                        p => p
                            .Terms(c => c
                                .Field(f => f.PartyServiceResourceId)
                                .MinDocCount(1)
                                .Size(1000000)
                            )
                    )
                    .Add("agg_dialog_per_month", d => d.DateHistogram(
                        y => y
                            .Field(x => x.CreatedAt)
                            .CalendarInterval(CalendarInterval.Month).Format("yyyy-MM")
                            .MinDocCount(1)
                        )
                    )
                ), cancellationToken);

        var endTime = Stopwatch.GetTimestamp();
        var elapsedTime = (endTime - startTime) / (double)Stopwatch.Frequency * 1000;
        Console.WriteLine($"Elapsed time: {elapsedTime} ms");

        Console.WriteLine(response);

        return elapsedTime;
    }
}
