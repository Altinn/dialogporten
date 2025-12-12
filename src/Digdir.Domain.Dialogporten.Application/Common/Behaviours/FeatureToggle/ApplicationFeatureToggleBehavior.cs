using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureToggle;

internal sealed class ApplicationFeatureToggleBehavior<TRequest, TResponse>(
    IMediator mediator,
    IApplicationFeatureToggle<TRequest, TResponse>? featureToggle = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        featureToggle is not null && featureToggle.IsEnabled
            ? FeatureToggle(request, cancellationToken)
            : next(cancellationToken);

    private async Task<TResponse> FeatureToggle(TRequest request, CancellationToken cancellationToken)
    {
        if (featureToggle is null || !featureToggle.IsEnabled)
        {
            throw new InvalidOperationException("FeatureToggle is disabled");
        }

        var toggledRequest = featureToggle.ConvertRequest(request);
        var toggledResponse = await mediator.Send(toggledRequest, cancellationToken);
        return featureToggle.ConvertResponse(toggledResponse);
    }
}

internal interface IApplicationFeatureToggle<in TRequest, out TResponse>
    where TRequest : IRequest<TResponse>
{
    bool IsEnabled { get; }
    IBaseRequest ConvertRequest(TRequest request);
    TResponse ConvertResponse(object? response);
}

internal abstract class AbstractApplicationFeatureToggle<TOldRequest, TOldResponse, TNewRequest, TNewResponse> : IApplicationFeatureToggle<TOldRequest, TOldResponse>
    where TOldRequest : IRequest<TOldResponse>
    where TNewRequest : IRequest<TNewResponse>
{
    public abstract bool IsEnabled { get; }

    IBaseRequest IApplicationFeatureToggle<TOldRequest, TOldResponse>.ConvertRequest(TOldRequest request) =>
        ConvertRequest(request);

    TOldResponse IApplicationFeatureToggle<TOldRequest, TOldResponse>.ConvertResponse(object? response) =>
        response is TNewResponse newResponse
            ? ConvertResponse(newResponse)
            : throw new InvalidOperationException("Invalid response type.");

    protected abstract TNewRequest ConvertRequest(TOldRequest request);
    protected abstract TOldResponse ConvertResponse(TNewResponse response);
}
