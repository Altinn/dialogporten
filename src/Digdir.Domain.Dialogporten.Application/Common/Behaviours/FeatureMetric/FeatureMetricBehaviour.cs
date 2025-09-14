using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed class FeatureMetricBehaviour<TRequest, TResponse>(
    IUser user,
    FeatureMetricRecorder featureMetricRecorder,
    IServiceResourceResolver<TRequest>? serviceResourceResolver = null,
    IHostEnvironment? hostEnvironment = null) // Optional for now to fix tests
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly FeatureMetricRecorder _featureMetricRecorder = featureMetricRecorder ?? throw new ArgumentNullException(nameof(featureMetricRecorder));
    private readonly IUser _user = user ?? throw new ArgumentNullException(nameof(user));
    private readonly IServiceResourceResolver<TRequest> _serviceResourceResolver = serviceResourceResolver ?? NullResourceResolver<TRequest>.Instance;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _user.GetPrincipal().TryGetOrganizationShortName(out var performingOrg);
        var resource = await _serviceResourceResolver.Resolve(request, cancellationToken);
        _featureMetricRecorder.Record(new(
            FeatureName: typeof(TRequest).FullName!,
            Environment: hostEnvironment?.EnvironmentName,
            PerformerOrg: performingOrg,
            OwnerOrg: resource?.OwnOrgShortName,
            ServiceResource: resource?.ResourceId));
        return await next(cancellationToken);
    }
}
