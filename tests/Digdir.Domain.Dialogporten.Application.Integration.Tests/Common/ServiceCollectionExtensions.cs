using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal static class ServiceCollectionExtensions
{
    internal static void ChangeUserPid(this IServiceCollection x, string pid)
    {
        x.RemoveAll<IUser>();

        var claims = IntegrationTestUser
            .GetDefaultClaims()
            .Where(y => y.Type != "pid")
            .Concat([new Claim("pid", pid)])
            .ToList();

        var newUser = new IntegrationTestUser(claims, addDefaultClaims: false);

        x.AddSingleton<IUser>(newUser);
    }
}
