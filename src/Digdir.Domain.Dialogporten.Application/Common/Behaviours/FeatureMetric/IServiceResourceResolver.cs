using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal interface IServiceResourceResolver<in TRequest>
{
    Task<ServiceResourceInformation?> Resolve(TRequest request, CancellationToken cancellationToken);
}

internal sealed class NullResourceResolver<TRequest> : IServiceResourceResolver<TRequest>
{
    public static readonly NullResourceResolver<TRequest> Instance = new();
    private NullResourceResolver() { }
    public Task<ServiceResourceInformation?> Resolve(TRequest request, CancellationToken cancellationToken) =>
        Task.FromResult<ServiceResourceInformation?>(null);
}
