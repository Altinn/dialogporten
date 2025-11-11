using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Domain.Common;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class SilentUpdateBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISilentUpdater
{
    private readonly IApplicationContext _applicationContext;

    public SilentUpdateBehaviour(IApplicationContext applicationContext)
    {
        _applicationContext = applicationContext;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.IsSilentUpdate)
        {
            _applicationContext.AddMetadata(Constants.IsSilentUpdate, bool.TrueString);
        }

        return await next(cancellationToken);
    }
}

public interface ISilentUpdater
{
    bool IsSilentUpdate => true;
}
