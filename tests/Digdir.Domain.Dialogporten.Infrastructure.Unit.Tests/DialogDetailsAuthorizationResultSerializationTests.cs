using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using MessagePack;
using MessagePack.Resolvers;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

// Pins round-tripping through the exact resolver the PDP cache (Altinn.Authorization, InfrastructureExtensions)
// is configured with. ContractlessStandardResolverAllowPrivate serializes private fields, so a lazily-built
// private field on DialogDetailsAuthorizationResult can silently break every cache write unless excluded.
public class DialogDetailsAuthorizationResultSerializationTests
{
    [Fact]
    public void Should_Round_Trip_Through_The_Pdp_Cache_Resolver_After_The_Lookup_Index_Is_Built()
    {
        var check = new AuthorizationCheck("read", AuthorizationResourceSpec.Main, ["urn:altinn:person:identifier-no:12345678901"]);
        var result = new DialogDetailsAuthorizationResult
        {
            AuthorizedChecks = [AuthorizedCheck.FullyPermitted(check)]
        };

        // Force the lazily-built index into existence before serializing, mirroring production usage where a
        // cached instance may have already answered queries before being written back to the distributed cache.
        Assert.True(result.HasAccess(check));

        var testToken = TestContext.Current.CancellationToken;
        var bytes = MessagePackSerializer.Serialize(result, ContractlessStandardResolverAllowPrivate.Options, testToken);
        var roundTripped = MessagePackSerializer.Deserialize<DialogDetailsAuthorizationResult>(
            bytes, ContractlessStandardResolverAllowPrivate.Options, testToken);

        Assert.True(roundTripped.HasAccess(check));
        Assert.True(roundTripped.HasReadAccessToMainResource());
    }

    [Fact]
    public void Should_Round_Trip_Restricted_Units_Through_The_Pdp_Cache_Resolver()
    {
        const string party = "urn:altinn:organization:identifier-no:713330310";
        var check = new AuthorizationCheck(
        [
            new AuthorizationUnit("read", AuthorizationResourceSpec.FromContext("urn:altinn:resource:r", "urn:altinn:task:Task_1"), party),
            new AuthorizationUnit("readall", AuthorizationResourceSpec.FromContext(null, null), party,
                AuthorizationAccessRestriction.ClientDelegation, "urn:altinn:organization:identifier-no:991825827")
        ]);
        var result = new DialogDetailsAuthorizationResult
        {
            AuthorizedChecks = [new AuthorizedCheck(check, [check.Units[1]])]
        };

        var testToken = TestContext.Current.CancellationToken;
        var bytes = MessagePackSerializer.Serialize(result, ContractlessStandardResolverAllowPrivate.Options, testToken);
        var roundTripped = MessagePackSerializer.Deserialize<DialogDetailsAuthorizationResult>(
            bytes, ContractlessStandardResolverAllowPrivate.Options, testToken);

        Assert.True(roundTripped.HasAccess(check));
        var authorizedCheck = Assert.Single(roundTripped.AuthorizedChecks);
        Assert.Equal(check, authorizedCheck.Check);
        Assert.Equal([check.Units[1]], authorizedCheck.PermittedUnits);
    }
}
