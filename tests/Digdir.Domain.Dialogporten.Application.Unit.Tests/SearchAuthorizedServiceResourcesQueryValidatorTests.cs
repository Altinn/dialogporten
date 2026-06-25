using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.ServiceResources.Queries.Search;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Microsoft.Extensions.Options;
using Xunit;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests;

public class SearchAuthorizedServiceResourcesQueryValidatorTests
{
    private const int MaxPartyFilterValues = 20;

    [Fact]
    public void Accepts_Party_Filter_At_The_Limit()
    {
        var query = new SearchAuthorizedServiceResourcesQuery { Parties = MakeParties(MaxPartyFilterValues) };

        var result = CreateValidator().Validate(query);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_Party_Filter_Above_The_Limit()
    {
        var query = new SearchAuthorizedServiceResourcesQuery { Parties = MakeParties(MaxPartyFilterValues + 1) };

        var result = CreateValidator().Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Accepts_Unfiltered_Request()
    {
        var result = CreateValidator().Validate(new SearchAuthorizedServiceResourcesQuery { Parties = null });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-party-urn")]
    [InlineData("urn:altinn:organization:identifier-no:123")]
    public void Rejects_Blank_Or_Invalid_Party_Value(string party)
    {
        // A supplied-but-blank/invalid filter must be rejected, not silently normalized to an unfiltered
        // (full-catalogue) request in the provider.
        var query = new SearchAuthorizedServiceResourcesQuery { Parties = [party] };

        var result = CreateValidator().Validate(query);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_Filter_With_A_Single_Blank_Among_Valid_Values()
    {
        var query = new SearchAuthorizedServiceResourcesQuery { Parties = [MakeParty(0), "", MakeParty(1)] };

        var result = CreateValidator().Validate(query);

        result.IsValid.Should().BeFalse();
    }

    // legacy-selfidentified identifiers are valid party identifiers with no checksum, so they keep the
    // valid-case fixtures simple and distinct without depending on Mod11-valid org/person numbers.
    private static string MakeParty(int i) => $"{AltinnSelfIdentifiedUserIdentifier.PrefixWithSeparator}user{i}";

    private static string[] MakeParties(int count) =>
        Enumerable.Range(0, count).Select(MakeParty).ToArray();

    private static SearchAuthorizedServiceResourcesQueryValidator CreateValidator() =>
        new(new StubOptionsSnapshot(new ApplicationSettings
        {
            Dialogporten = null!,
            Limits = new LimitsSettings
            {
                EndUserSearch = new EndUserSearchQueryLimits { MaxPartyFilterValues = MaxPartyFilterValues }
            }
        }));

    private sealed class StubOptionsSnapshot(ApplicationSettings value) : IOptionsSnapshot<ApplicationSettings>
    {
        public ApplicationSettings Value => value;
        public ApplicationSettings Get(string? name) => value;
    }
}
