using Cocona;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncPolicy;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncSubjectMap;
using Digdir.Domain.Dialogporten.Janitor.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Digdir.Domain.Dialogporten.Janitor;

internal static class Commands
{
    private static readonly string[] Environments = ["TT02", "PROD"];
    internal static CoconaApp AddJanitorCommands(this CoconaApp app)
    {
        app.AddCommand("sync-subject-resource-mappings", async (
                [FromService] CoconaAppContext ctx,
                [FromService] ISender application,
                [Option('s')] DateTimeOffset? since,
                [Option('b')] int? batchSize)
            =>
            {
                var result = await application.Send(
                    new SyncSubjectMapCommand { Since = since, BatchSize = batchSize },
                    ctx.CancellationToken);
                return result.Match(
                    success => 0,
                    validationError => -1);
            });

        app.AddCommand("sync-resource-policy-information", async (
                [FromService] CoconaAppContext ctx,
                [FromService] ISender application,
                [Option('s')] DateTimeOffset? since,
                [Option('c')] int? numberOfConcurrentRequests)
            =>
            {
                var result = await application.Send(
                    new SyncPolicyCommand { Since = since, NumberOfConcurrentRequests = numberOfConcurrentRequests },
                    ctx.CancellationToken);
                return result.Match(
                    success => 0,
                    validationError => -1);
            });

        app.AddCommand("aggregate-metrics", async (
                [FromService] CoconaAppContext ctx,
                [FromService] AzureMonitorService azureMonitorService,
                [FromService] MetricsAggregationService aggregationService,
                [FromService] ParquetFileService parquetService,
                [FromService] AzureStorageService storageService,
                [FromService] ILogger<CoconaApp> logger,
                [Option('d')] DateOnly? targetDate)
            =>
            {
                try
                {
                    var date = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
                    logger.LogInformation("Starting metrics aggregation for date {Date}", date);

                    var allRecords = new List<MetricsRecord>();
                    var environments = Environments;

                    foreach (var environment in environments)
                    {
                        logger.LogInformation("Querying metrics for environment {Environment}", environment);
                        var records = await azureMonitorService.QueryCostMetricsAsync(date, environment, ctx.CancellationToken);
                        allRecords.AddRange(records);
                    }

                    var aggregatedRecords = aggregationService.AggregateMetrics(allRecords);
                    var parquetData = await parquetService.GenerateParquetFileAsync(aggregatedRecords, ctx.CancellationToken);
                    var fileName = ParquetFileService.GetFileName(date);

                    await storageService.UploadParquetFileAsync(parquetData, fileName, ctx.CancellationToken);

                    logger.LogInformation("Successfully completed metrics aggregation for {Date}. Generated {FileName} with {RecordCount} records",
                        date, fileName, aggregatedRecords.Count);

                    return 0;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to aggregate metrics for date {Date}", targetDate);
                    return -1;
                }
            });

        return app;
    }
}
