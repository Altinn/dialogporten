﻿using Microsoft.AspNetCore.Authorization;

namespace Digdir.Domain.Dialogporten.GraphQL.Common.Authorization;

/// <summary>
/// This authorization handler will bypass all requirements
/// </summary>
public sealed class AllowAnonymousHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var requirement in context.PendingRequirements)
        {
            //Simply pass all requirements
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
