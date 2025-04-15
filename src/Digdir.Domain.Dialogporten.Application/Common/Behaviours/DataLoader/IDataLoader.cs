using System.Runtime.CompilerServices;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;

internal interface IDataLoader<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IRequireDataLoader
{
    Task<object?> Load(TRequest request, CancellationToken cancellationToken);
}

internal interface ITypedDataLoader<in TRequest, TResponse, TData> :
    IDataLoader<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IRequireDataLoader
{
    new Task<TData?> Load(TRequest request, CancellationToken cancellationToken);
}

internal abstract class TypedDataLoader<TRequest, TResponse, TData, TSelf> :
    ITypedDataLoader<TRequest, TResponse, TData>
    where TSelf : TypedDataLoader<TRequest, TResponse, TData, TSelf>
    where TRequest : IRequest<TResponse>, IRequireDataLoader
{

    async Task<object?> IDataLoader<TRequest, TResponse>.Load(TRequest request, CancellationToken cancellationToken)
        => await Load(request, cancellationToken);
    public abstract Task<TData?> Load(TRequest request, CancellationToken cancellationToken);
    public static TData? GetPreloadedData(TRequest request) => (TData?)request.PreloadedData[nameof(TSelf)];
}
