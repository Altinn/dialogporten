using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed class FeatureMetricBehaviour<TRequest, TResponse>(
    IUser user,
    FeatureMetricRecorder featureMetricRecorder,
    IFeatureMetricServiceResourceResolver<TRequest> featureMetricServiceResourceResolver,
    IHostEnvironment? hostEnvironment = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly FeatureMetricRecorder _featureMetricRecorder = featureMetricRecorder ?? throw new ArgumentNullException(nameof(featureMetricRecorder));
    private readonly IUser _user = user ?? throw new ArgumentNullException(nameof(user));
    private readonly IFeatureMetricServiceResourceResolver<TRequest> _featureMetricServiceResourceResolver = featureMetricServiceResourceResolver ?? throw new ArgumentNullException(nameof(featureMetricServiceResourceResolver));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var principal = _user.GetPrincipal();
        principal.TryGetOrganizationShortName(out var performingOrg);
        var hasAdminScope = principal.HasScope(AuthorizationScope.ServiceOwnerAdminScope);
        var resource = await _featureMetricServiceResourceResolver.Resolve(request, cancellationToken);
        _featureMetricRecorder.Record(new(
            FeatureName: typeof(TRequest).FullName!,
            HasAdminScope: hasAdminScope,
            Environment: hostEnvironment?.EnvironmentName,
            PerformerOrg: performingOrg,
            OwnerOrg: resource?.OwnOrgShortName,
            ServiceResource: resource?.ResourceId));
        return await next(cancellationToken);
    }
}
