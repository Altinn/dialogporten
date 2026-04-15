using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
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
        var callerOrgNr = TryGetPrincipal(out var principal)
                          && principal.TryGetConsumerOrgNumber(out var orgNumber)
            ? orgNumber
            : null;

        var hasAdminScope = principal?.HasScope(AuthorizationScope.ServiceOwnerAdminScope) ?? false;
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

    private bool TryGetPrincipal([NotNullWhen(true)] out ClaimsPrincipal? principal)
    {
        try
        {
            principal = _user.GetPrincipal();
        }
        catch (InvalidOperationException)
        {
            principal = null;
            return false;
        }
        return true;
    }
}
