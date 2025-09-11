using System.Diagnostics;
using System.Diagnostics.Metrics;
using Digdir.Domain.Dialogporten.Application.Common.Behaviours.DataLoader;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using FluentValidation;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly IDataLoaderContext _dataLoaderContext;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators, IDataLoaderContext dataLoaderContext)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        _dataLoaderContext = dataLoaderContext ?? throw new ArgumentNullException(nameof(dataLoaderContext));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        foreach (var (key, value) in _dataLoaderContext)
        {
            context.RootContextData.Add(key, value);
        }

        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults
            .SelectMany(x => x.Errors)
            .Where(x => x is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        if (OneOfExtensions.TryConvertToOneOf<TResponse>(new ValidationError(failures), out var result))
        {
            return result;
        }

        throw new ValidationException(failures);
    }
}
