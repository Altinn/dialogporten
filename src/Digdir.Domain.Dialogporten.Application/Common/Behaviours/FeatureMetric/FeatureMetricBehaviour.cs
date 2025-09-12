using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed class FeatureMetricBehaviour<TRequest, TResponse>(
    IHostEnvironment hostEnvironment,
    IUser user,
    IFeatureMetricRecorder featureMetricRecorder,
    IServiceResourceResolver<TRequest> resourceResolver)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IFeatureMetricRecorder _featureMetricRecorder = featureMetricRecorder ?? throw new ArgumentNullException(nameof(featureMetricRecorder));
    private readonly IHostEnvironment _hostEnvironment = hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment));
    private readonly IUser _user = user ?? throw new ArgumentNullException(nameof(user));
    private readonly IServiceResourceResolver<TRequest> _resourceResolver = resourceResolver ?? throw new ArgumentNullException(nameof(resourceResolver));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var environment = _hostEnvironment.EnvironmentName;
        _user.GetPrincipal().TryGetOrganizationShortName(out var performingOrg);
        var resource = await _resourceResolver.Resolve(request, cancellationToken);
        _featureMetricRecorder.Record(new(name, environment, performingOrg, resource?.OwnOrgShortName, resource?.ResourceId));
        return await next(cancellationToken);
    }
}
