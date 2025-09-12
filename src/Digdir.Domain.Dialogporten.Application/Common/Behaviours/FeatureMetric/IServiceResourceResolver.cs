using Digdir.Domain.Dialogporten.Application.Externals;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal interface IServiceResourceResolver<in TRequest>
{
    Task<ServiceResourceInformation?> Resolve(TRequest request, CancellationToken cancellationToken);
}
