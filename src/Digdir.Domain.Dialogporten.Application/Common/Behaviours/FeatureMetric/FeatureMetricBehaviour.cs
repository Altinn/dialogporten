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
        var name = typeof(TRequest).Name;
        var environment = hostEnvironment?.EnvironmentName ?? "Unknown";
        var audience = DetermineAudience(typeof(TRequest));
        _user.GetPrincipal().TryGetOrganizationShortName(out var performingOrg);
        var resource = await _serviceResourceResolver.Resolve(request, cancellationToken);
        _featureMetricRecorder.Record(new(name, environment, performingOrg, resource?.OwnOrgShortName, resource?.ResourceId, null, null, audience));
        return await next(cancellationToken);
    }

    private static string? DetermineAudience(Type requestType)
    {
        var namespaceName = requestType.Namespace;
        if (namespaceName is null)
        {
            return null;
        }

        // Check if the namespace contains ServiceOwner or EndUser
        if (namespaceName.Contains(".ServiceOwner.", StringComparison.OrdinalIgnoreCase))
        {
            return "ServiceOwner";
        }

        if (namespaceName.Contains(".EndUser.", StringComparison.OrdinalIgnoreCase))
        {
            return "EndUser";
        }

        return null;
    }
}
