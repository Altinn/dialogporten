using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

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

    /// <summary>
    /// Removes any existing <see cref="IAltinnAuthorization"/> and adds a substitute that can be configured.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="configure"></param>
    internal static void ConfigureAltinnAuthorization(this IServiceCollection x, Action<IAltinnAuthorization> configure)
    {
        x.RemoveAll<IAltinnAuthorization>();

        var altinnAuthorizationSubstitute = Substitute.For<IAltinnAuthorization>();
        configure(altinnAuthorizationSubstitute);

        x.AddSingleton(altinnAuthorizationSubstitute);
    }

    /// <summary>
    /// Configures the <see cref="IAltinnAuthorization"/> to return a specific result for search authorization.
    /// </summary>
    /// <param name="altinnAuthorization"></param>
    /// <param name="result"></param>
    internal static void ConfigureGetAuthorizedResourcesForSearch(
        this IAltinnAuthorization altinnAuthorization,
        DialogSearchAuthorizationResult result) =>
        altinnAuthorization.GetAuthorizedResourcesForSearch(
                Arg.Any<List<string>>(),
                Arg.Any<List<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(result);
}
