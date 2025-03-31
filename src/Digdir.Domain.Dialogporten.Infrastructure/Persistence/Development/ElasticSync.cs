using System.Diagnostics;
using System.Runtime.CompilerServices;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Elastic.Clients.Elasticsearch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Development;

internal sealed class ElasticSync : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public ElasticSync(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var runSync = false;
        if (!runSync)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDialogDbContext>();

        var elasticClient = new ElasticsearchClient();

        var elasticPartyIndexName = nameof(ElasticParty).ToLowerInvariant();
        var indexExists = await elasticClient.Indices.ExistsAsync(elasticPartyIndexName, cancellationToken);

        if (!indexExists.Exists)
        {
            var response = await elasticClient.Indices.CreateAsync<ElasticParty>(index => index
                    .Index(elasticPartyIndexName)
                    .Mappings(m => m.Properties(p => p
                        .Keyword(k => k.Party)
                        .Nested("dialogs", np => np
                            .Properties(np => np
                                .Keyword(d => d.Dialogs.First().ServiceResource)
                                .Keyword(d => d.Dialogs.First().DialogId)
                                .Date(d => d.Dialogs.First().CreatedAt)
                            )
                        )
                    )),
                cancellationToken);

            if (!response.IsValidResponse)
            {
                Console.WriteLine("Index creation failed.");
                return;
            }
        }

        const int batchSize = 10000;
        var distinctPartyQuery = dbContext.Dialogs
            .Select(d => d.Party)
            .Distinct()
            .Select(x => new ElasticParty
            {
                Party = x,
                Dialogs = new List<ElasticDialog>()
            });

        var distinctParties = await distinctPartyQuery
            .ToListAsync(cancellationToken);

        var bulkPartiesStartTimestamp = Stopwatch.GetTimestamp();

        var bulkCreateParties = await elasticClient.BulkAsync(b => b
            .Index(elasticPartyIndexName)
            .IndexMany(distinctParties, (bu, party) => bu
                .Id(party.Party)
                .Index(elasticPartyIndexName)
            ), cancellationToken);


        if (bulkCreateParties.Errors)
        {
            Console.WriteLine("Failed to create party documents.");
        }

        var bulkPartiesEndTimestamp = Stopwatch.GetTimestamp();
        var bulkPartiesElapsedTime = (bulkPartiesEndTimestamp - bulkPartiesStartTimestamp) / (double)Stopwatch.Frequency * 1000;
        Console.WriteLine($"Bulk parties elapsed time: {bulkPartiesElapsedTime} ms");

        // await SyncPartyToElasticsearchAsync(elasticPartyIndexName, dbContext, elasticClient, cancellationToken);

        // const int batchSize = 10000;
        var processedCount = 0;

        var syncStartTimestamp = Stopwatch.GetTimestamp();
        try
        {
            Guid? lastSeenId = null;
            var moreData = true;

            while (moreData)
            {
                var query = dbContext.Dialogs
                    .AsNoTracking();

                if (lastSeenId.HasValue)
                {
                    query = query.Where(d => d.Id > lastSeenId.Value); // cleaner than CompareTo
                }

                query = query.OrderBy(d => d.Id)
                    .Take(batchSize);


                var dialogs = await query
                    .Select(d => new ElasticParty
                    {
                        Party = d.Party,
                        Dialogs = new()
                        {
                            new ElasticDialog
                            {
                                CreatedAt = d.CreatedAt,
                                DialogId = d.Id,
                                ServiceResource = d.ServiceResource
                            }
                        }
                    })
                    .ToListAsync(cancellationToken);

                moreData = dialogs.Count == batchSize;
                if (moreData)
                {
                    lastSeenId = dialogs[^1].Dialogs[0].DialogId;
                }

                var batchStartTimestamp = Stopwatch.GetTimestamp();

                var bulkUpdateDialogs = await elasticClient.BulkAsync(b => b
                    .Index(elasticPartyIndexName)
                    .UpdateMany(dialogs, (bu, d) => bu
                            .Id(d.Party)
                            .Script(s => s
                                .Source(@"
                    if (!ctx._source.dialogs.contains(params.newDialog)) {
                        ctx._source.dialogs.add(params.newDialog);
                    }")
                                .Params(p => p
                                    .Add("newDialog", new ElasticDialog
                                    {
                                        CreatedAt = d.Dialogs[0].CreatedAt,
                                        DialogId = d.Dialogs[0].DialogId,
                                        ServiceResource = d.Dialogs[0].ServiceResource
                                    })
                                )
                            )
                            .RetriesOnConflict(50) // Avoid race conditions
                    ), cancellationToken);

                if (bulkUpdateDialogs.Errors)
                {
                    Console.WriteLine("Failed to update dialog documents.");
                    foreach (var item in bulkUpdateDialogs.ItemsWithErrors)
                    {
                        Console.WriteLine($"Error: {item.Error?.Reason}, Id: {item.Error?.StackTrace}");
                    }
                }

                // Update count
                processedCount += dialogs.Count;

                // Only log every 10 batches (50,000 rows)
                if (processedCount % (batchSize * 10) != 0) continue;
                var batchEndTimestamp = Stopwatch.GetTimestamp();
                var batchElapsedTime = (batchEndTimestamp - batchStartTimestamp) / (double)Stopwatch.Frequency * 1000;
                Console.WriteLine($"Batch elapsed time: {batchElapsedTime} ms, processed: {processedCount}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }


        var endTimestamp = Stopwatch.GetTimestamp();
        var elapsedTime = (endTimestamp - syncStartTimestamp) / (double)Stopwatch.Frequency * 1000;
        var elapsedTimeSpan = TimeSpan.FromMilliseconds(elapsedTime);
        var hours = elapsedTimeSpan.Hours;
        var minutes = elapsedTimeSpan.Minutes;
        var seconds = elapsedTimeSpan.Seconds;

        Console.WriteLine($"Elapsed time: {hours}h {minutes}m {seconds}s");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
