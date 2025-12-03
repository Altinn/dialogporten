using MediatR;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class DialogportenMaintenanceModeBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IOptionsSnapshot<ApplicationSettings> _appSettings;

    public DialogportenMaintenanceModeBehaviour(IOptionsSnapshot<ApplicationSettings> appSettings)
    {
        _appSettings = appSettings;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_appSettings.Value.FeatureToggle.DialogportenMaintenanceMode)
        {
            // add proper return type to all requests? 🤔
            throw new DialogportenUnavailableException();
        }

        return await next(cancellationToken);
    }
}

internal class DialogportenUnavailableException : Exception;
