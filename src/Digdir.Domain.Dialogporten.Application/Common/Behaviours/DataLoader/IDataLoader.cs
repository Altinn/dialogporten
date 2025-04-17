using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;

/// <summary>
/// Defines a contract for data loaders that can preload data for a MediatR request
/// before the request handler executes.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
internal interface IDataLoader<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Gets a unique key that identifies the data loader.
    /// </summary>
    /// <returns>A string representing the unique key.</returns>
    string GetKey();

    /// <summary>
    /// Loads data for the given request asynchronously.
    /// </summary>
    /// <param name="request">The request for which data is being loaded.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the loaded data.</returns>
    Task<object?> Load(TRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// Extends <see cref="IDataLoader{TRequest,TResponse}"/> with strong typing for the loaded data.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <typeparam name="TData">The type of data being loaded.</typeparam>
internal interface ITypedDataLoader<in TRequest, TResponse, TData> :
    IDataLoader<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Loads strongly-typed data for the given request asynchronously.
    /// </summary>
    /// <param name="request">The request for which data is being loaded.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the loaded data of type <typeparamref name="TData"/>.</returns>
    new Task<TData?> Load(TRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves preloaded data from the provided context.
    /// </summary>
    /// <param name="context">The context containing preloaded data.</param>
    /// <returns>The preloaded data of type <typeparamref name="TData"/>, or null if not found.</returns>
    static abstract TData? GetPreloadedData(IDataLoaderContext context);
}

/// <summary>
/// Base implementation of <see cref="ITypedDataLoader{TRequest,TResponse,TData}"/> that provides
/// common functionality for concrete data loaders.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <typeparam name="TData">The type of data being loaded.</typeparam>
/// <typeparam name="TSelf">The concrete implementing type (CRTP pattern).</typeparam>
internal abstract class TypedDataLoader<TRequest, TResponse, TData, TSelf> :
    ITypedDataLoader<TRequest, TResponse, TData>
    where TSelf : TypedDataLoader<TRequest, TResponse, TData, TSelf>
    where TRequest : IRequest<TResponse>
{
    private static readonly string Key = typeof(TSelf).FullName!;

    /// <inheritdoc/>
    public string GetKey() => Key;

    /// <inheritdoc/>
    async Task<object?> IDataLoader<TRequest, TResponse>.Load(TRequest request, CancellationToken cancellationToken)
        => await Load(request, cancellationToken);

    /// <inheritdoc/>
    public abstract Task<TData?> Load(TRequest request, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public static TData? GetPreloadedData(IDataLoaderContext context) => (TData?)context.Get(Key);
}
