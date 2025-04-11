using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Domain.Common.Exceptions;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

/// <summary>
/// This behaviour is used to ensure that the domain context is valid and return the correct response to the presentation layer if it is not.
/// <para>
/// The behaviour will return <see cref="DomainError"/> if <typeparamref name="TResponse"/> implements
/// <see cref="OneOf.IOneOf"/> containing <see cref="DomainError"/> on invalid domain state.
/// </para>
/// <para>
/// The behaviour will throw a <see cref="DomainException"/> if <typeparamref name="TResponse"/> does not
/// implement <see cref="OneOf.IOneOf"/> containing <see cref="DomainError"/> on invalid domain state.
/// </para>
/// </summary>
/// <remarks>
/// This behaviour <b>will not</b> prevent saving changes to the database. That must be handled elsewhere.
/// </remarks>
internal sealed class DomainContextBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDomainContext _domainContext;

    public DomainContextBehaviour(IDomainContext domainContext)
    {
        _domainContext = domainContext ?? throw new ArgumentNullException(nameof(domainContext));
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        TResponse? response;

        try
        {
            response = await next(cancellationToken);
        }
        catch (DomainException ex)
        {
            if (OneOfExtensions.TryConvertToOneOf(new DomainError(ex.Errors), out response))
            {
                return response;
            }

            throw;
        }

        if (_domainContext.IsValid)
        {
            return response;
        }

        var domainFailures = _domainContext.Pop();

        return OneOfExtensions.TryConvertToOneOf(new DomainError(domainFailures), out response)
            ? response
            : throw new DomainException(domainFailures);
    }
}
