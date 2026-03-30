using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed class FeatureMetricBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly FeatureMetricRecorder _featureMetricRecorder;
    private readonly IUser _user;
    private readonly IFeatureMetricServiceResourceResolver<TRequest> _featureMetricServiceResourceResolver;
    private readonly IHostEnvironment? _hostEnvironment;

    public FeatureMetricBehaviour(
        IUser user,
        FeatureMetricRecorder featureMetricRecorder,
        IFeatureMetricServiceResourceResolver<TRequest> featureMetricServiceResourceResolver,
        IHostEnvironment? hostEnvironment = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(featureMetricRecorder);
        ArgumentNullException.ThrowIfNull(featureMetricServiceResourceResolver);

        _user = user;
        _featureMetricRecorder = featureMetricRecorder;
        _featureMetricServiceResourceResolver = featureMetricServiceResourceResolver;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var principal = _user.GetPrincipal();
        principal.TryGetConsumerOrgNumber(out var callerOrgNr);

        var hasAdminScope = principal.HasScope(AuthorizationScope.ServiceOwnerAdminScope);
        var resource = await _featureMetricServiceResourceResolver.Resolve(request, cancellationToken);

        _featureMetricRecorder.Record(new(
            FeatureName: typeof(TRequest).FullName!,
            HasAdminScope: hasAdminScope,
            Environment: _hostEnvironment?.EnvironmentName,
            CallerOrg: callerOrgNr,
            OwnerOrg: resource?.OwnerOrgNumber,
            ServiceResource: resource?.ResourceId));

        return await next(cancellationToken);
    }
}
