using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.WebApi.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using UserType = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogUserType.Values;
using Policy = Digdir.Domain.Dialogporten.WebApi.Common.Authorization.AuthorizationPolicy;

namespace Digdir.Domain.Dialogporten.WebApi.Common.Authentication;

public sealed class UserTypeValidationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Dictionary<string, List<UserType>> ValidUserTypesForPolicy = new()
    {
        { Policy.EndUser, [UserType.Person, UserType.SystemUser, UserType.IdportenSelfIdentifiedUser, UserType.AltinnSelfIdentifiedUser, UserType.FeideUser] },
        { Policy.ServiceProvider, [UserType.ServiceOwner, UserType.ServiceOwnerOnBehalfOfPerson] },
        { Policy.ServiceProviderSearch, [UserType.ServiceOwner, UserType.ServiceOwnerOnBehalfOfPerson] },
        { Policy.ServiceProviderAdmin, [UserType.ServiceOwner] }
    };

    public UserTypeValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity is { IsAuthenticated: true })
        {
            if (!IsValidUserTypeForEndpoint(context, out var validUserTypes))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                var response = context.GetResponseOrDefault(
                    context.Response.StatusCode,
                    [
                        new("Type",
                            $"The request was authenticated, but we were unable to determine valid user type in order to authorize the request. Valid user types for this endpoint are: {string.Join(", ", validUserTypes)}")
                    ]
                );
                await context.Response.WriteAsJsonAsync(response, response.GetType());

                return;
            }
        }

        await _next(context);
    }

    private static bool IsValidUserTypeForEndpoint(
        HttpContext context,
        [NotNullWhen(false)] out List<UserType>? validUserTypes)
    {
        var policy = context.GetEndpoint()?
            .Metadata.GetOrderedMetadata<AuthorizeAttribute>() is { Count: > 0 } metadata
            ? metadata[0].Policy
            : null;

        if (policy is null || !ValidUserTypesForPolicy.TryGetValue(policy, out validUserTypes))
        {
            validUserTypes = null;
            return true;
        }

        var (userType, _) = context.User.GetUserType();
        return validUserTypes.Contains(userType);
    }
}

public static class UserTypeValidationMiddlewareExtensions
{
    public static IApplicationBuilder UseUserTypeValidation(this IApplicationBuilder app)
        => app.UseMiddleware<UserTypeValidationMiddleware>();
}
