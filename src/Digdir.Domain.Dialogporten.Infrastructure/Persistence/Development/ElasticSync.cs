using System.Diagnostics;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
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

        var indexName = nameof(ElasticDialog).ToLowerInvariant();
        var indexExistsResponse = await elasticClient.Indices
            .ExistsAsync(indexName, cancellationToken);
        if (!indexExistsResponse.Exists)
        {
            var createIndexResponse = await elasticClient.Indices
                .CreateAsync<ElasticDialog>(index =>
                    index.Index(indexName)
                        .Mappings(m =>
                            m.Properties(p => p
                                // .Keyword(k => k.DialogId)
                                .Keyword(d => d.PartyServiceResourceId)
                                .Date(d => d.CreatedAt)
                            )), cancellationToken);
            if (!createIndexResponse.IsValidResponse)
            {
                Console.WriteLine("fail");
                return;
            }
        }

        const int batchSize = 10000;
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
                    .Select(d => new ElasticDialog
                    {
                        DialogId = d.Id,
                        PartyServiceResourceId = d.Party + d.ServiceResource,
                        CreatedAt = d.CreatedAt
                    })
                    .ToListAsync(cancellationToken);

                moreData = dialogs.Count == batchSize;
                if (moreData)
                {
                    lastSeenId = dialogs[^1].DialogId;
                }

                var batchStartTimestamp = Stopwatch.GetTimestamp();

                var batchResponse = await elasticClient.BulkAsync(b => b
                .Index(indexName)
                .IndexMany(dialogs, (b, d) => b
                .Id(d.DialogId.ToString())
                ), cancellationToken);
                if (batchResponse.Errors)
                {
                    Console.WriteLine("fail");
                }
                // Update count
                processedCount += dialogs.Count;

                // Only log every 10 batches (50,000 rows)
                if (processedCount % (batchSize * 10) != 0) continue;
                var batchEndTimestamp = Stopwatch.GetTimestamp();
                var batchElapsedTime = (batchEndTimestamp - batchStartTimestamp) / (double)Stopwatch.Frequency * 1000;
                Console.WriteLine($"Batch elapsed time: {batchElapsedTime} ms, processed: {processedCount}");
            }

            // var skip = 0;
            // var moreData = true;
            //
            // while (moreData)
            // {
            //     var dialogs = await dbContext.Dialogs
            //         .AsNoTracking()
            //         .OrderBy(d => d.Id)
            //         .Skip(skip)
            //         .Take(batchSize)
            //         .Select(d => new ElasticDialog
            //         {
            //             DialogId = d.Id,
            //             PartyServiceResourceId = d.Party + d.ServiceResource,
            //             CreatedAt = d.CreatedAt
            //         })
            //         .ToListAsync(cancellationToken);
            //
            //     moreData = dialogs.Count == batchSize;
            //     skip += dialogs.Count;
            //
            //     var batchStartTimestamp = Stopwatch.GetTimestamp();
            //     var batchResponse = await elasticClient.BulkAsync(b => b
            //         .Index(indexName)
            //         .IndexMany(dialogs, (b, d) => b
            //             .Id(d.DialogId.ToString())
            //         ), cancellationToken);
            //     if (batchResponse.Errors)
            //     {
            //         Console.WriteLine("fail");
            //     }
            //     // only log every 50 batches
            //     if (skip % (batchSize * 10) != 0) continue;
            //     var batchEndTimestamp = Stopwatch.GetTimestamp();
            //     var batchElapsedTime = (batchEndTimestamp - batchStartTimestamp) / (double)Stopwatch.Frequency * 1000;
            //     Console.WriteLine($"Batch elapsed time: {batchElapsedTime} ms, skip: {skip}");
            // }

            // await foreach (var dialog in dialogStream.WithCancellation(cancellationToken))
            // {
            //     Console.WriteLine($"Fetched dialog {dialog.DialogId}");
            //     batch.Add(dialog);
            //
            //     if (batch.Count >= batchSize)
            //     {
            //         var batchStartTimestamp = Stopwatch.GetTimestamp();
            //         Console.WriteLine("Sending batch to Elasticsearch...");
            //         var batchResponse = await elasticClient.BulkAsync(b => b
            //             .Index(indexName)
            //             .IndexMany(batch, (b, d) => b
            //                 .Id(d.DialogId.ToString())
            //             ), cancellationToken);
            //         Console.WriteLine("Got response from Elasticsearch.");
            //         if (batchResponse.Errors)
            //         {
            //             Console.WriteLine("fail");
            //         }
            //         batch.Clear();
            //         var batchEndTimestamp = Stopwatch.GetTimestamp();
            //         var batchElapsedTime = (batchEndTimestamp - batchStartTimestamp) / (double)Stopwatch.Frequency * 1000;
            //         Console.WriteLine($"Batch elapsed time: {batchElapsedTime} ms");
            //     }
            // }
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
