using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.ServiceResources.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using AuthConstants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Infrastructure.HealthChecks;

/// <summary>
/// The readiness warmup phases. Registered with Altinn.AspNet.HealthChecks.Warmup, which owns the
/// hosted service, the phase sequencing and timeout budget, the warmup state and the readiness
/// health check. Each phase receives the scoped service provider shared by the whole warmup run.
/// </summary>
/// <remarks>
/// Optional phases deliberately contain no exception handling: the library logs and continues past a
/// failing optional phase, so catching here would only hide the failure.
/// </remarks>
internal static partial class WarmupPhases
{
    internal static async Task WarmupDbPoolAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var settings = services.GetRequiredService<IOptions<InfrastructureSettings>>().Value.Warmup;
        var dataSource = services.GetRequiredService<NpgsqlDataSource>();
        var options = new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = settings.DbConnectionParallelism
        };

        await Parallel.ForEachAsync(
            Enumerable.Range(0, settings.DbConnectionsToOpen),
            options,
            async (_, token) =>
            {
                await using var connection = await dataSource.OpenConnectionAsync(token);
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(token);
            });
    }

    internal static async Task WarmupEfModelAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<DialogDbContext>();
        await dbContext.DialogStatuses
            .AsNoTracking()
            .Where(x => x.Id == DialogStatus.Values.Completed)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal static async Task WarmupServiceResourceMetadataAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var sender = services.GetRequiredService<ISender>();
        await sender.Send(new GetServiceResourceMetadataQuery(), cancellationToken);
    }

    internal static async Task WarmupEndUserSearchAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var settings = services.GetRequiredService<IOptions<InfrastructureSettings>>().Value.Warmup;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(WarmupPhases));

        if (string.IsNullOrWhiteSpace(settings.EndUserPid))
        {
            EndUserSearchSkippedMissingPid(logger);
            return;
        }

        using var _ = AmbientUserPrincipal.Use(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimsPrincipalExtensions.ScopeClaim, "digdir:dialogporten"),
            new Claim(ClaimsPrincipalExtensions.PidClaim, settings.EndUserPid),
            new Claim(ClaimsPrincipalExtensions.IdportenAuthLevelClaim, AuthConstants.IdportenLoaSubstantial)
        ], "WarmupAuth")));

        var sender = services.GetRequiredService<ISender>();
        var result = await sender.Send(new SearchDialogQuery
        {
            Party = [$"urn:altinn:person:identifier-no:{settings.EndUserPid}"],
            Limit = 5
        }, cancellationToken);

        if (result.IsT0 && result.AsT0.Items.Count == 0)
        {
            EndUserSearchReturnedNoRows(logger);
        }
        else if (!result.IsT0)
        {
            EndUserSearchReturnedNonSuccess(logger, result.Value.GetType().Name);
        }
    }

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Skipping end-user search warmup because Infrastructure:Warmup:EndUserPid is not configured.")]
    private static partial void EndUserSearchSkippedMissingPid(ILogger logger);

    [LoggerMessage(EventId = 4, Level = LogLevel.Warning, Message = "End-user search warmup returned no rows.")]
    private static partial void EndUserSearchReturnedNoRows(ILogger logger);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "End-user search warmup returned {ResultType}.")]
    private static partial void EndUserSearchReturnedNonSuccess(ILogger logger, string resultType);
}
