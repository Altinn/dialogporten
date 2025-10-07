using Cocona;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncPolicy;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncSubjectMap;
using Digdir.Domain.Dialogporten.Janitor.Services;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Janitor;

internal static class Commands
{
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
                [FromService] IHostEnvironment hostEnvironment,
                [FromService] ApplicationInsightsService applicationInsightsService,
                [FromService] MetricsAggregationService aggregationService,
                [FromService] ParquetFileService parquetService,
                [FromService] AzureStorageService storageService,
                [FromService] ILogger<CoconaApp> logger,
                [FromService] IOptions<MetricsAggregationOptions> options,
                [Option('d')] DateOnly? targetDate,
                [Option('e', Description = "Environment(s) to query. Can be specified multiple times (e.g., -e TT02 -e PROD)")] string[]? environments)
            =>
            {
                try
                {
                    var date = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
                    var config = options.Value;

                    logger.LogInformation("Host Environment: {Environment}", hostEnvironment.EnvironmentName);

                    // Use environment parameter if provided, otherwise use config
                    var targetEnvironments = environments != null && environments.Length > 0
                        ? environments.ToList()
                        : config.Environments;

                    logger.LogInformation("Configuration - SkipUpload: {SkipUpload}, Target Environments: [{Environments}]",
                        config.SkipUpload, string.Join(", ", targetEnvironments));
                    logger.LogInformation("Starting metrics aggregation for date {Date}",
                        date);

                    var allRecords = new List<FeatureMetricRecord>();

                    foreach (var env in targetEnvironments)
                    {
                        logger.LogInformation("Querying feature metrics from Application Insights for environment {Environment}", env);

                        var records = await applicationInsightsService.QueryFeatureMetricsAsync(date, env, ctx.CancellationToken);

                        allRecords.AddRange(records);
                    }

                    var aggregatedRecords = aggregationService.AggregateFeatureMetrics(allRecords);
                    var parquetData = await parquetService.GenerateParquetFileAsync(aggregatedRecords, ctx.CancellationToken);

                    // Include environment(s) in filename when saving locally
                    var fileName = config.SkipUpload
                        ? ParquetFileService.GetFileName(date, targetEnvironments)
                        : ParquetFileService.GetFileName(date);

                    if (config.SkipUpload)
                    {
                        await File.WriteAllBytesAsync(fileName, parquetData, ctx.CancellationToken);
                        logger.LogInformation("Saved parquet file locally as {FileName} with {RecordCount} records ({FileSize} bytes)",
                            fileName, aggregatedRecords.Count, parquetData.Length);
                    }
                    else
                    {
                        await storageService.UploadParquetFileAsync(parquetData, fileName, ctx.CancellationToken);
                        logger.LogInformation("Successfully completed metrics aggregation for {Date}. Generated {FileName} with {RecordCount} records",
                            date, fileName, aggregatedRecords.Count);
                    }

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
