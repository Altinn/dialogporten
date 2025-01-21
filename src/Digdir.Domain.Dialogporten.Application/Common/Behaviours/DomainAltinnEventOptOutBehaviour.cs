using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Common.Exceptions;
using MediatR;
using AuthConstants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class DomainAltinnEventOptOutBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, IAltinnEventDisabler
{
    private readonly IDomainEventContext _domainEventContext;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public DomainAltinnEventOptOutBehaviour(IDomainEventContext domainEventContext, IUserResourceRegistry userResourceRegistry)
    {
        _domainEventContext = domainEventContext;
        _userResourceRegistry = userResourceRegistry;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.DisableAltinnEvents && !_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            var forbidden = new Forbidden(AuthConstants.DisableAltinnEventsRequiresAdminScope);
            return OneOfExtensions.TryConvertToOneOf<TResponse>(forbidden, out var result)
                ? result
                : throw new ForbiddenException(AuthConstants.DisableAltinnEventsRequiresAdminScope);
        }

        if (request.DisableAltinnEvents)
        {
            _domainEventContext.AddMetadata(Constants.DisableAltinnEvents, bool.TrueString);
        }

        return await next();
    }
}

public interface IAltinnEventDisabler
{
    bool DisableAltinnEvents { get; }
}
