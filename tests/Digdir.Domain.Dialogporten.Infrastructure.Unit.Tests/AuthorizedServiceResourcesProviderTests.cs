using System.Diagnostics;
using System.Security.Claims;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AuthorizedServiceResourcesProviderTests
{
    private const string Pid = "22834498646";
    private const string PartyA = "urn:altinn:organization:identifier-no:111111111";
    private const string ReferencedResource = "urn:altinn:resource:referenced";
    private const string UnreferencedResource = "urn:altinn:resource:unreferenced";

    [Fact]
    public async Task Prunes_Resources_Not_Referenced_By_Dialogporten()
    {
        var authorization = new CountingAltinnAuthorization(new DialogSearchAuthorizationResult
        {
            ResourcesByParties = new Dictionary<string, HashSet<string>>
            {
                [PartyA] = [ReferencedResource, UnreferencedResource]
            }
        });
        // Only ReferencedResource is referenced by Dialogporten for PartyA.
        var referenceRepository = new StubPartyResourceReferenceRepository(new Dictionary<string, HashSet<string>>
        {
            [PartyA] = [ReferencedResource]
        });
        var provider = new AuthorizedServiceResourcesProvider(authorization, CreateUserWithPid(Pid), referenceRepository);

        var result = await provider.GetAuthorizedServiceResourcesByParty(TestContext.Current.CancellationToken);

        // The unreferenced resource is pruned even though the authorized set has only 2 resources (well below
        // the default pruning threshold), proving pruning is unconditional for this endpoint.
        result.Should().ContainKey(PartyA);
        result[PartyA].Should().BeEquivalentTo(ReferencedResource);
        // DialogIds are not needed for this endpoint, so they are not resolved.
        authorization.LastIncludeDialogIds.Should().BeFalse();
    }

    [Fact]
    public async Task Throws_For_Principal_Without_End_User_Party_Identifier()
    {
        var authorization = new CountingAltinnAuthorization(new DialogSearchAuthorizationResult());
        var provider = new AuthorizedServiceResourcesProvider(
            authorization,
            new StubUser(new ClaimsPrincipal(new ClaimsIdentity())),
            new StubPartyResourceReferenceRepository(new()));

        var act = () => provider.GetAuthorizedServiceResourcesByParty(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnreachableException>();
        authorization.CallCount.Should().Be(0);
    }

    private static StubUser CreateUserWithPid(string pid) =>
        new(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimsPrincipalExtensions.PidClaim, pid)])));

    private sealed class StubUser(ClaimsPrincipal principal) : IUser
    {
        public ClaimsPrincipal GetPrincipal() => principal;
    }

    private sealed class StubPartyResourceReferenceRepository(Dictionary<string, HashSet<string>> referencedByParty)
        : IPartyResourceReferenceRepository
    {
        public Task<IReadOnlyCollection<string>> GetReferencedResources(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Dictionary<string, HashSet<string>>> GetReferencedResourcesByParty(
            IReadOnlyCollection<string> parties,
            IReadOnlyCollection<string> resources,
            CancellationToken cancellationToken) =>
            Task.FromResult(referencedByParty
                .Where(x => parties.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase));

        public Task InvalidateCachedReferencesForParty(string party, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class CountingAltinnAuthorization(DialogSearchAuthorizationResult result) : IAltinnAuthorization
    {
        public int CallCount { get; private set; }
        public bool? LastIncludeDialogIds { get; private set; }

        public Task<DialogSearchAuthorizationResult> GetAuthorizedResourcesForSearch(
            List<string> constraintParties,
            List<string> constraintServiceResources,
            bool includeDialogIds = true,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastIncludeDialogIds = includeDialogIds;
            return Task.FromResult(result);
        }

        public Task<DialogDetailsAuthorizationResult> GetDialogDetailsAuthorization(
            DialogEntity dialogEntity, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AuthorizedPartiesResult> GetAuthorizedParties(
            IPartyIdentifier authenticatedParty, bool flatten = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<AuthorizedPartiesResult> GetAuthorizedPartiesForLookup(
            IPartyIdentifier authenticatedParty, List<string> constraintParties, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<bool> HasListAuthorizationForDialog(
            DialogEntity dialog, CancellationToken cancellationToken) => throw new NotSupportedException();

        public bool UserHasRequiredAuthLevel(int minimumAuthenticationLevel) => throw new NotSupportedException();

        public Task<bool> UserHasRequiredAuthLevel(
            string serviceResource, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
