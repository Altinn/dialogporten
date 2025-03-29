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

        var dialogStream = dbContext.Dialogs
            .AsNoTracking()
            .Select(d => new ElasticDialog
            {
                DialogId = d.Id,
                PartyServiceResourceId = d.Party + d.ServiceResource,
                CreatedAt = d.CreatedAt
            })
            .AsAsyncEnumerable();

        const int batchSize = 1000;
        var batch = new List<ElasticDialog>(batchSize);

        var syncStartTimestamp = Stopwatch.GetTimestamp();
        await foreach (var dialog in dialogStream.WithCancellation(cancellationToken))
        {
            batch.Add(dialog);

            if (batch.Count >= batchSize)
            {
                // await IndexBatchAsync(batch, elasticClient, indexName, cancellationToken);
                var batchResponse = await elasticClient.BulkAsync(b => b
                    .Index(indexName)
                    .IndexMany(batch, (b, d) => b
                        .Id(d.DialogId.ToString())
                    ), cancellationToken);
                if (batchResponse.Errors)
                {
                    Console.WriteLine("fail");
                }
                batch.Clear();
            }
        }

        var endTimestamp = Stopwatch.GetTimestamp();
        var elapsedTime = (endTimestamp - syncStartTimestamp) / (double)Stopwatch.Frequency * 1000;
        // write it out in hours, minutes, and seconds
        var elapsedTimeSpan = TimeSpan.FromMilliseconds(elapsedTime);
        var hours = elapsedTimeSpan.Hours;
        var minutes = elapsedTimeSpan.Minutes;
        var seconds = elapsedTimeSpan.Seconds;

        Console.WriteLine($"Elapsed time: {hours}h {minutes}m {seconds}s");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
