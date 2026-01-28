using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using Mediator;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncSubjectMap;

public sealed class SyncSubjectMapCommand : IRequest<SyncSubjectMapResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public DateTimeOffset? Since { get; set; }
    public int? BatchSize { get; set; }
}

[GenerateOneOf]
public sealed partial class SyncSubjectMapResult : OneOfBase<Success, ValidationError>;

internal sealed partial class SyncSubjectMapCommandHandler : IRequestHandler<SyncSubjectMapCommand, SyncSubjectMapResult>
{
    private const int DefaultBatchSize = 1000;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly ISubjectResourceRepository _subjectResourceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SyncSubjectMapCommandHandler> _logger;

    public SyncSubjectMapCommandHandler(
        IResourceRegistry resourceRegistry,
        ISubjectResourceRepository subjectResourceRepository,
        IUnitOfWork unitOfWork,
        ILogger<SyncSubjectMapCommandHandler> logger)
    {
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _subjectResourceRepository = subjectResourceRepository ?? throw new ArgumentNullException(nameof(subjectResourceRepository));
        _unitOfWork = unitOfWork;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<SyncSubjectMapResult> Handle(SyncSubjectMapCommand request, CancellationToken cancellationToken)
    {
        // Get the last updated timestamp from parameter, or the database (with a time skew), or use a default
        var lastUpdated = request.Since
            ?? await _subjectResourceRepository.GetLastUpdatedAt(
                timeSkew: TimeSpan.FromMicroseconds(1),
                cancellationToken: cancellationToken);

        LogFetchingUpdatedSubjectResourcesSince(lastUpdated);

        try
        {
            var mergeCount = 0;
            var syncTime = DateTimeOffset.Now;
            await _unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
            await foreach (var resourceBatch in _resourceRegistry
                .GetUpdatedSubjectResources(lastUpdated, request.BatchSize ?? DefaultBatchSize, cancellationToken))
            {
                var mergeableSubjectResources = resourceBatch
                    .Select(x => x.ToMergeableSubjectResource(syncTime))
                    .GroupBy(x => new { x.Subject, x.Resource })
                    .Select(g => g.OrderByDescending(x => x.UpdatedAt).First())
                    .ToList();

                var batchMergeCount = await _subjectResourceRepository.Merge(mergeableSubjectResources, cancellationToken);
                LogSubjectResourcesAddedToTransaction(batchMergeCount);
                mergeCount += batchMergeCount;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (mergeCount > 0)
            {
                LogSuccessfullySyncedSubjectResources(mergeCount);
            }
            else
            {
                _logger.LogInformation("Subject-resources are already up to date.");
            }

            return new Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to sync subject-resources. Rolling back transaction.");
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Fetching updated subject-resources since {LastUpdated:O}.")]
    private partial void LogFetchingUpdatedSubjectResourcesSince(DateTimeOffset lastUpdated);

    [LoggerMessage(Level = LogLevel.Information, Message = "{BatchMergeCount} subject-resources added to transaction.")]
    private partial void LogSubjectResourcesAddedToTransaction(int batchMergeCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully synced {UpdatedAmount} total subject-resources. Changes committed.")]
    private partial void LogSuccessfullySyncedSubjectResources(int updatedAmount);
}
