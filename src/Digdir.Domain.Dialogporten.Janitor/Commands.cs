using Cocona;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncPolicy;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncSubjectMap;
using Digdir.Domain.Dialogporten.Application.Features.V1.Search.Commands.ReindexDialogSearch;
using Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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

        app.AddCommand("reindex-dialogsearch", async (
                [FromService] CoconaAppContext ctx,
                [FromService] ISender application,
                [Option('f', Description = "Force full reindex (seed all)")] bool full,
                [Option('s', Description = "Reindex dialogs with Dialog.UpdatedAt >= <timestamp> (UTC)")] DateTimeOffset? since,
                [Option('r', Description = "Resume previously started reindex (do not reseed)")] bool resume,
                [Option('o', Description = "Seed only stale dialogs (missing/outdated)")] bool staleOnly,
                [Option("stale-first", Description = "Prioritize stale dialogs first")] bool staleFirst,
                [Option('b', Description = "Batch size per worker (default 1000)")] int? batchSize,
                [Option('w', Description = "Number of parallel workers (default 1)")] int? workers,
                [Option("throttle-ms", Description = "Sleep between batches per worker (ms)")] int? throttleMs,
                [Option("work-mem-bytes", Description = "work_mem per worker (default 268435456 bytes = 256MB)")] long? workMemBytes)
            =>
            {
                var result = await application.Send(new ReindexDialogSearchCommand
                {
                    Full = full,
                    Since = since,
                    Resume = resume,
                    StaleOnly = staleOnly,
                    StaleFirst = staleFirst,
                    BatchSize = batchSize,
                    Workers = workers,
                    ThrottleMs = throttleMs,
                    WorkMemBytes = workMemBytes
                }, ctx.CancellationToken);

                return result.Match(
                    success => 0,
                    validationError => -1);
            });

        app.AddCommand("aggregate-cost-metrics", async (
                [FromService] CoconaAppContext ctx,
                [FromService] IConfiguration config,
                [FromService] IHostEnvironment env,
                [FromService] CostMetricsAggregationOrchestrator orchestrator,
                [FromService] ILogger<CoconaApp> logger,
                [Option('d', Description = "Target date for metrics aggregation (format: YYYY-MM-DD or DD/MM/YYYY). Defaults to yesterday in Norwegian time.")] DateOnly? targetDateInput,
                [Option('s', Description = "Skip uploading to Azure Storage and save file locally instead")] bool skipUpload = false)
            =>
            {
                var targetDate = targetDateInput ?? NorwegianTimeConverter.GetYesterday();

                logger.LogInformation("Host Environment: {Environment}", env.EnvironmentName);

                var localDevSettings = config.GetLocalDevelopmentSettings();
                if (env.IsDevelopment() && localDevSettings.UseLocalMetricsAggregationStorage)
                {
                    skipUpload = true;
                }

                var result = await orchestrator.AggregateCostMetricsForDateOnlyAsync(targetDate, skipUpload, ctx.CancellationToken);

                return result.Match(
                    success => 0,
                    failure => -1);
            });

        return app;
    }
}
