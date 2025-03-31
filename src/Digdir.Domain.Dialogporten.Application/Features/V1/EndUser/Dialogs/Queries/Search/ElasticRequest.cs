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
    private readonly IAltinnAuthorization _altinnAuthorization;

    public ElasticThingyHandler(
        IAltinnAuthorization altinnAuthorization)
    {
        _altinnAuthorization = altinnAuthorization ?? throw new ArgumentNullException(nameof(altinnAuthorization));
    }

    public async Task<ElasticResult> Handle(ElasticRequest request, CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.GetTimestamp();

        var authorizedResources = await _altinnAuthorization.GetAuthorizedResourcesForSearch(
            request.Party,
            request.ServiceResource,
            cancellationToken: cancellationToken);

        var parties = authorizedResources.ResourcesByParties
            .Select(k => k.Key)
            .ToList();

        var mustPreFilters = parties
            .Select(party => (Action<QueryDescriptor<ElasticParty>>)(q => q
                .Match(m => m
                    .Field(f => f.Party)
                    .Query(party))))
            .ToArray();

        if (mustPreFilters.Length == 0)
        {
            throw new DivideByZeroException("bruh no party 4 u");
        }
        var elasticClient = new ElasticsearchClient();

        var response = await elasticClient
            .SearchAsync<ElasticParty>(search => search
                .Index(nameof(ElasticParty).ToLowerInvariant())
                .Size(0)
                .Query(query => query
                    .Bool(b => b.Should(mustPreFilters))
                )
                .Aggregations(ags => ags
                    .Add("agg_party",
                        p => p
                            .Terms(c => c
                                .Field(f => f.Party)
                                .MinDocCount(1)
                                .Size(1_000_000)
                            )
                            .Aggregations(a => a
                                .Add("nested_dialogs",
                                    n => n
                                        .Nested(n => n.Path(f => f.Dialogs))
                                        .Aggregations(na => na
                                            .Add("agg_serviceResource",
                                                sr => sr
                                                    .Terms(t => t
                                                        .Field("dialogs.serviceResource")
                                                        .MinDocCount(1)
                                                        .Size(1_000_000)))))))), cancellationToken);

        var endTime = Stopwatch.GetTimestamp();
        var elapsedTime = (endTime - startTime) / (double)Stopwatch.Frequency * 1000;
        Console.WriteLine($"Elapsed time: {elapsedTime} ms");

        Console.WriteLine(response);

        return elapsedTime;
    }
}
