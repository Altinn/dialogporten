using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Repositories;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class PartyResourceRepositoryTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    private const string Party1UnprefixedIdentifier = "313130983";
    private const string Party2UnprefixedIdentifier = "19895597581";
    private const string Party3UnprefixedIdentifier = "someusername";
    private const string Party4UnprefixedIdentifier = "foo@bar.com";

    private const string Resource1UnprefixedIdentifier = "resource-a";
    private const string Resource2UnprefixedIdentifier = "resource-b";
    private const string Resource3UnprefixedIdentifier = "resource-c";

    private const string Party1 = $"urn:altinn:organization:identifier-no:{Party1UnprefixedIdentifier}";
    private const string Party2 = $"urn:altinn:person:identifier-no:{Party2UnprefixedIdentifier}";
    private const string Party3 = $"urn:altinn:person:legacy-selfidentified:{Party3UnprefixedIdentifier}";
    private const string Party4 = $"urn:altinn:person:idporten-email:{Party4UnprefixedIdentifier}";

    private const string Resource1 = $"urn:altinn:resource:{Resource1UnprefixedIdentifier}";
    private const string Resource2 = $"urn:altinn:resource:{Resource2UnprefixedIdentifier}";
    private const string Resource3 = $"urn:altinn:resource:{Resource3UnprefixedIdentifier}";

    [Fact]
    public async Task GetReferencedResourcesByParty_ShouldReturnOnlyRequestedResources()
    {
        await SeedPartyResourceData(CancellationToken.None);

        using var scope = Application.GetServiceProvider().CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPartyResourceReferenceRepository>();

        var result = await sut.GetReferencedResourcesByParty(
            [Party1, Party2, Party3, Party4],
            [Resource2, Resource3],
            CancellationToken.None);

        Assert.Equal(4, result.Count);

        Assert.True(result.TryGetValue(Party1, out var party1Resources));
        Assert.Single(party1Resources);
        Assert.Contains(Resource2, party1Resources);

        Assert.True(result.TryGetValue(Party2, out var party2Resources));
        Assert.Single(party2Resources);
        Assert.Contains(Resource2, party2Resources);

        Assert.True(result.TryGetValue(Party3, out var party3Resources));
        Assert.Single(party3Resources);
        Assert.Contains(Resource2, party3Resources);

        Assert.True(result.TryGetValue(Party4, out var party4Resources));
        Assert.Single(party4Resources);
        Assert.Contains(Resource2, party4Resources);
    }

    [Fact]
    public async Task GetReferencedResourcesByParty_ShouldHandleWhitespaceAndCaseInsensitiveResourceMatch()
    {
        await SeedPartyResourceData(CancellationToken.None);

        using var scope = Application.GetServiceProvider().CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPartyResourceReferenceRepository>();

        var result = await sut.GetReferencedResourcesByParty(
            [Party1, " ", Party1],
            [" ", Resource2.ToUpperInvariant()],
            CancellationToken.None);

        Assert.Single(result);
        Assert.True(result.TryGetValue(Party1, out var resources));
        Assert.Single(resources);
        Assert.Contains(Resource2, resources, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetReferencedResourcesByParty_ShouldReturnEmpty_WhenNoRequestedResourceMatches()
    {
        await SeedPartyResourceData(CancellationToken.None);

        using var scope = Application.GetServiceProvider().CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPartyResourceReferenceRepository>();

        var result = await sut.GetReferencedResourcesByParty(
            [Party1, Party2],
            ["urn:altinn:resource:resource-not-found"],
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetReferencedResourcesByParty_ShouldThrow_WhenPartyUrnIsUnsupported()
    {
        await SeedPartyResourceData(CancellationToken.None);

        using var scope = Application.GetServiceProvider().CreateScope();
        var sut = scope.ServiceProvider.GetRequiredService<IPartyResourceReferenceRepository>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetReferencedResourcesByParty(
            ["not-a-party-urn"],
            [Resource1],
            CancellationToken.None));
    }

    private async Task SeedPartyResourceData(CancellationToken cancellationToken)
    {
        using var scope = Application.GetServiceProvider().CreateScope();
        var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText =
            """
            INSERT INTO partyresource."Party" ("ShortPrefix", "UnprefixedPartyIdentifier")
            VALUES ('o', @party1), ('p', @party2), ('i', @party3), ('e', @party4);

            INSERT INTO partyresource."Resource" ("UnprefixedResourceIdentifier")
            VALUES (@resource1), (@resource2), (@resource3);

            WITH pairs("ShortPrefix", "UnprefixedPartyIdentifier", "UnprefixedResourceIdentifier") AS (
                VALUES
                    ('o', @party1, @resource1),
                    ('o', @party1, @resource2),
                    ('p', @party2, @resource2),
                    ('i', @party3, @resource2),
                    ('e', @party4, @resource2)
            )
            INSERT INTO partyresource."PartyResource" ("PartyId", "ResourceId")
            SELECT p."Id", r."Id"
            FROM pairs pair
            INNER JOIN partyresource."Party" p
                ON p."ShortPrefix" = pair."ShortPrefix"
               AND p."UnprefixedPartyIdentifier" = pair."UnprefixedPartyIdentifier"
            INNER JOIN partyresource."Resource" r
                ON r."UnprefixedResourceIdentifier" = pair."UnprefixedResourceIdentifier";
            """;

        command.Parameters.AddWithValue("party1", Party1UnprefixedIdentifier);
        command.Parameters.AddWithValue("party2", Party2UnprefixedIdentifier);
        command.Parameters.AddWithValue("party3", Party3UnprefixedIdentifier);
        command.Parameters.AddWithValue("party4", Party4UnprefixedIdentifier);
        command.Parameters.AddWithValue("resource1", Resource1UnprefixedIdentifier);
        command.Parameters.AddWithValue("resource2", Resource2UnprefixedIdentifier);
        command.Parameters.AddWithValue("resource3", Resource3UnprefixedIdentifier);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
