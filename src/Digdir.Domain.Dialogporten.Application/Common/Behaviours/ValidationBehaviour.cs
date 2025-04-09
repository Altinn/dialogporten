using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using FluentValidation;
using Mediator;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators ?? throw new ArgumentNullException(nameof(validators));
    }

    public async ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
        MessageHandlerDelegate<TRequest, TResponse> next)
    {
        if (!_validators.Any())
        {
            return await next(request, cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = validationResults
            .SelectMany(x => x.Errors)
            .Where(x => x is not null)
            .ToList();

        if (failures.Count == 0)
        {
            return await next(request, cancellationToken);
        }

        if (OneOfExtensions.TryConvertToOneOf<TResponse>(new ValidationError(failures), out var result))
        {
            return result;
        }

        throw new ValidationException(failures);
    }
}
