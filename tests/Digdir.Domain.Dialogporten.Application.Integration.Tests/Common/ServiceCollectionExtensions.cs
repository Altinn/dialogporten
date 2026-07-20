using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal static class ServiceCollectionExtensions
{
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

    internal static void ConfigureDialogDetailsAuthorizationResult(
        this IServiceCollection services,
        DialogDetailsAuthorizationResult result)
    {
        services.ConfigureAltinnAuthorization(altinnAuthorization =>
        {
            altinnAuthorization.GetDialogDetailsAuthorization(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(result));
            altinnAuthorization.UserHasRequiredAuthLevel(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(true);
            altinnAuthorization.UserHasRequiredAuthLevel(Arg.Any<int>())
                .Returns(true);
            altinnAuthorization.HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
                .Returns(true);
        });
    }

    /// <summary>
    /// Configures the <see cref="IAltinnAuthorization"/> to return a specific result for search authorization.
    /// </summary>
    /// <param name="altinnAuthorization"></param>
    /// <param name="result"></param>
    internal static void ConfigureGetAuthorizedResourcesForSearch(
        this IAltinnAuthorization altinnAuthorization,
        DialogSearchAuthorizationResult result)
    {
        altinnAuthorization.GetAuthorizedResourcesForSearch(
                Arg.Any<List<string>>(),
                Arg.Any<List<string>>(),
                Arg.Any<bool>(),
                Arg.Any<int?>(),
                Arg.Any<CancellationToken>())
            // Honor the pushed-down party constraint (first argument), like the production
            // AltinnAuthorizationClient and LocalDevelopmentAltinnAuthorization do, so callers that pass a party
            // filter get a result scoped to those parties.
            .Returns(callInfo =>
            {
                var constraintParties = callInfo.ArgAt<List<string>>(0);
                if (constraintParties.Count == 0)
                {
                    return result;
                }

                return new DialogSearchAuthorizationResult
                {
                    ResourcesByParties = result.ResourcesByParties
                        .Where(kv => constraintParties.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                        .ToDictionary(kv => kv.Key, kv => kv.Value),
                    DialogIds = result.DialogIds
                };
            });

        // The authorized-service-resources provider counts the caller's parties (via GetAuthorizedParties) to
        // decide whether to fall back to the full catalogue. Return the same parties so the count stays below the
        // fallback limit and resolution proceeds to the configured GetAuthorizedResourcesForSearch result.
        // (IPartyIdentifier has static abstract members and can't be used with Arg.Any<>, so use ReturnsForAnyArgs.)
        altinnAuthorization.GetAuthorizedParties(null!, default, default)
            .ReturnsForAnyArgs(new AuthorizedPartiesResult
            {
                AuthorizedParties = result.ResourcesByParties.Keys.Select(CreateAuthorizedParty).ToList()
            });
    }

    private static AuthorizedParty CreateAuthorizedParty(string party) => new()
    {
        Party = party,
        PartyUuid = Guid.NewGuid(),
        PartyId = 0,
        Name = "Party",
        DateOfBirth = null,
        PartyType = AuthorizedPartyType.Organization,
        IsDeleted = false,
        HasKeyRole = false,
        IsCurrentEndUser = false,
        IsMainAdministrator = false,
        IsAccessManager = false,
        HasOnlyAccessToSubParties = false,
        AuthorizedResources = [],
        AuthorizedRolesAndAccessPackages = [],
        AuthorizedInstances = []
    };
}
