using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

internal sealed class FeatureMetricBehaviour<TRequest, TResponse>(
    IUser user,
    FeatureMetricRecorder featureMetricRecorder,
    IServiceProvider serviceProvider,
    IHostEnvironment? hostEnvironment = null) // Optional for now to fix tests
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly FeatureMetricRecorder _featureMetricRecorder = featureMetricRecorder ?? throw new ArgumentNullException(nameof(featureMetricRecorder));
    private readonly IHostEnvironment? _hostEnvironment = hostEnvironment;
    private readonly IUser _user = user ?? throw new ArgumentNullException(nameof(user));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var environment = _hostEnvironment?.EnvironmentName ?? "Unknown";
        var audience = DetermineAudience(typeof(TRequest));
        _user.GetPrincipal().TryGetOrganizationShortName(out var performingOrg);

        ServiceResourceInformation? resource = null;

        // Check if the request implements IDialogIdQuery and use the generic resolver
        if (request is IDialogIdQuery)
        {
            var dialogResolver = _serviceProvider.GetService<IServiceResourceResolver<IDialogIdQuery>>();
            if (dialogResolver != null)
            {
                resource = await dialogResolver.Resolve((IDialogIdQuery)request, cancellationToken);
            }
        }
        else
        {
            // Try to get a specific resolver for this request type
            var specificResolver = _serviceProvider.GetService<IServiceResourceResolver<TRequest>>();
            if (specificResolver != null)
            {
                resource = await specificResolver.Resolve(request, cancellationToken);
            }
        }

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
