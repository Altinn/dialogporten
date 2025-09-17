using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Externals;
using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using OneOf.Types;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncPolicy;

public sealed class SyncPolicyCommand : IRequest<SyncPolicyResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    public DateTimeOffset? Since { get; set; }
    public int? NumberOfConcurrentRequests { get; set; }
}

[GenerateOneOf]
public sealed partial class SyncPolicyResult : OneOfBase<Success, ValidationError>;

internal sealed class SyncPolicyCommandHandler : IRequestHandler<SyncPolicyCommand, SyncPolicyResult>
{
    private const int DefaultNumberOfConcurrentRequests = 15;
    private readonly IResourceRegistry _resourceRegistry;
    private readonly IResourcePolicyInformationRepository _resourcePolicyMetadataRepository;
    private readonly ILogger<SyncPolicyCommandHandler> _logger;

    public SyncPolicyCommandHandler(
        IResourceRegistry resourceRegistry,
        IResourcePolicyInformationRepository resourcePolicyMetadataRepository,
        ILogger<SyncPolicyCommandHandler> logger)
    {
        _resourceRegistry = resourceRegistry ?? throw new ArgumentNullException(nameof(resourceRegistry));
        _resourcePolicyMetadataRepository = resourcePolicyMetadataRepository ?? throw new ArgumentNullException(nameof(resourcePolicyMetadataRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SyncPolicyResult> Handle(SyncPolicyCommand request,
        CancellationToken cancellationToken)
    {
        // Get the last updated timestamp from parameter, or the database (with a time skew), or use a default
        var lastUpdated = request.Since
                          ?? await _resourcePolicyMetadataRepository.GetLastUpdatedAt(
                              timeSkew: TimeSpan.FromMicroseconds(1),
                              cancellationToken: cancellationToken);

        _logger.LogInformation("Fetching updated resource policy information since {LastUpdated:O}.", lastUpdated);

        try
        {
            var syncTime = DateTimeOffset.Now;
            var updatedResourcePolicyInformation = await _resourceRegistry
                               .GetUpdatedResourcePolicyInformation(lastUpdated, request.NumberOfConcurrentRequests ?? DefaultNumberOfConcurrentRequests, cancellationToken);

            var mergeableResourcePolicyInformation = updatedResourcePolicyInformation
                .Select(x => x.ToResourcePolicyInformation(syncTime))
                .ToList();
            var mergeCount = await _resourcePolicyMetadataRepository.Merge(mergeableResourcePolicyInformation, cancellationToken);
            _logger.LogInformation("{MergeCount} copies of resource policy information updated.", mergeCount);

            if (mergeCount > 0)
            {
                _logger.LogInformation("Successfully synced information from {MergeCount} policies", mergeCount);
            }
            else
            {
                _logger.LogInformation("Resource policy information are already up-to-date.");
            }

            return new Success();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to sync resource policy information.");
            throw;
        }
    }
}
