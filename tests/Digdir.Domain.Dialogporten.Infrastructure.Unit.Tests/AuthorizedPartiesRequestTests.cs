using System.Diagnostics.CodeAnalysis;
using Digdir.Domain.Dialogporten.Domain.Parties.Abstractions;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class AuthorizedPartiesRequestTests
{
    [Fact]
    public void GenerateCacheKey_ShouldChange_WhenIncludeFlagsDiffer()
    {
        var identifier = new TestPartyIdentifier();

        var defaultKey = new AuthorizedPartiesRequest(identifier).GenerateCacheKey();
        var includeResourcesKey = new AuthorizedPartiesRequest(identifier, includeResources: true).GenerateCacheKey();

        Assert.NotEqual(defaultKey, includeResourcesKey);
    }

    [Fact]
    public void GenerateCacheKey_ShouldChange_WhenPartyFilterDiffers()
    {
        var identifier = new TestPartyIdentifier();
        var partyFilter = new List<AuthorizedPartyFilter>
        {
            new()
            {
                Type = TestPartyIdentifier.Prefix,
                Value = "789"
            }
        };

        var defaultKey = new AuthorizedPartiesRequest(identifier).GenerateCacheKey();
        var filterKey = new AuthorizedPartiesRequest(identifier, partyFilter: partyFilter).GenerateCacheKey();

        Assert.NotEqual(defaultKey, filterKey);
    }

    private sealed class TestPartyIdentifier : IPartyIdentifier
    {
        public TestPartyIdentifier(string id = "123")
        {
            Id = id;
            FullId = PrefixWithSeparator + id;
        }

        public string FullId { get; }
        public string Id { get; }
        public static string Prefix => "urn:altinn:test";
        public static string PrefixWithSeparator => Prefix + PartyIdentifier.Separator;

        public static bool TryParse(ReadOnlySpan<char> value, [NotNullWhen(true)] out IPartyIdentifier? identifier)
        {
            identifier = new TestPartyIdentifier(value.ToString());
            return true;
        }
    }
}
