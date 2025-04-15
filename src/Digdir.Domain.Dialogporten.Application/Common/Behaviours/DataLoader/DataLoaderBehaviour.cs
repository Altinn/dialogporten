using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;

internal sealed class DataLoaderBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IRequireDataLoader
{
    private readonly IEnumerable<IDataLoader<TRequest, TResponse>> _loaders;

    public DataLoaderBehaviour(IEnumerable<IDataLoader<TRequest, TResponse>> loaders)
    {
        _loaders = loaders;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Should we support parallel data loaders? If so we need a mechanism to
        // serialize some data loaders, i.e. multiple data loaders targeting ef
        // core (which does not support parallel queries. We also need to
        // change from Dictionary<string, object?> to
        // ConcurrentDictionary<string, object?>
        foreach (var loader in _loaders)
        {
            request.PreloadedData[loader.GetKey()] = await loader.Load(request, cancellationToken);
        }

        return await next(cancellationToken);
    }
}
