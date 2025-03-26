using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Common.Exceptions;
using MediatR;
using AuthConstants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class SilentUpdateBeaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISilentUpdater
{
    private readonly ISilentUpdateContext _silentUpdateContext;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public SilentUpdateBeaviour(ISilentUpdateContext silentUpdateContext, IUserResourceRegistry userResourceRegistry)
    {
        _silentUpdateContext = silentUpdateContext;
        _userResourceRegistry = userResourceRegistry;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request.IsSilentUpdate && !_userResourceRegistry.IsCurrentUserServiceOwnerAdmin())
        {
            var forbidden = new Forbidden(AuthConstants.SilentUpdateRequiresAdminScope);
            return OneOfExtensions.TryConvertToOneOf<TResponse>(forbidden, out var result)
                ? result
                : throw new ForbiddenException(AuthConstants.SilentUpdateRequiresAdminScope);
        }

        if (request.IsSilentUpdate)
        {
            _silentUpdateContext.AddMetadata(Constants.IsSilentUpdate, bool.TrueString);
        }

        return await next();
    }
}

public interface ISilentUpdater
{
    bool IsSilentUpdate { get; }
}
