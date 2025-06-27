using Digdir.Domain.Dialogporten.Application.Common.Context;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Common.Exceptions;
using MediatR;
using AuthConstants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours;

internal sealed class SilentUpdateBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ISilentUpdater
{
    private readonly IApplicationContext _applicationContext;
    private readonly IUserResourceRegistry _userResourceRegistry;

    public SilentUpdateBehaviour(IApplicationContext applicationContext, IUserResourceRegistry userResourceRegistry)
    {
        _applicationContext = applicationContext;
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
            _applicationContext.AddMetadata(Constants.IsSilentUpdate, bool.TrueString);
        }

        return await next(cancellationToken);
    }
}

public interface ISilentUpdater
{
    bool IsSilentUpdate => true;
}
