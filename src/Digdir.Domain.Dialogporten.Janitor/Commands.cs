using Cocona;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncPolicy;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncSubjectMap;
using Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;
using MediatR;
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

        app.AddCommand("aggregate-cost-metrics", async (
                [FromService] CoconaAppContext ctx,
                [FromService] IHostEnvironment hostEnvironment,
                [FromService] CostMetricsAggregationOrchestrator orchestrator,
                [FromService] ILogger<CoconaApp> logger,
                [Option('d', Description = "Target date for metrics aggregation (format: YYYY-MM-DD or DD/MM/YYYY). Defaults to yesterday in Norwegian time.")] DateOnly? targetDateInput,
                [Option('s', Description = "Skip uploading to Azure Storage and save file locally instead")] bool skipUpload = false)
            =>
            {
                var targetDate = targetDateInput ?? NorwegianTimeConverter.GetYesterday();

                logger.LogInformation("Host Environment: {Environment}", hostEnvironment.EnvironmentName);

                var result = await orchestrator.AggregateCostMetricsForDateOnlyAsync(targetDate, skipUpload, ctx.CancellationToken);

                return result.Match(
                    success => 0,
                    failure => -1);
            });

        return app;
    }
}
