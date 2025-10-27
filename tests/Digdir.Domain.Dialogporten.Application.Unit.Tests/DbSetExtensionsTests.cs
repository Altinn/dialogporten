using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests;

public class DbSetExtensionsTests
{
    [Fact]
    public void PrefilterAuthorizedDialogs_GeneratesExpectedSql_ForGroupedParties()
    {
        // Arrange
        var authorizedResources = new DialogSearchAuthorizationResult
        {
            DialogIds = [Guid.CreateVersion7()],
            ResourcesByParties = new Dictionary<string, HashSet<string>>
            {
                { "Party1", ["Resource1", "Resource2"] },
                { "Party2", ["Resource1", "Resource2"] },
                { "Party3", ["Resource1", "Resource2", "Resource3"] },
                { "Party4", ["Resource3"] },
                { "Party5", ["Resource4"] }
            }
        };

        var expectedSql = """
                           SELECT *
                           FROM "Dialog"
                           WHERE "Id" = ANY(@p0)
                             OR (
                                "Party" = ANY(@p1)
                                AND "ServiceResource" = ANY(@p2)
                             )
                             OR (
                                "Party" = ANY(@p3)
                                AND "ServiceResource" = ANY(@p4)
                             )
                             OR (
                                "Party" = ANY(@p5)
                                AND "ServiceResource" = ANY(@p6)
                             )
                             OR (
                                "Party" = ANY(@p7)
                                AND "ServiceResource" = ANY(@p8)
                             )
                           """;
        var expectedParameters = new object[]
        {
            authorizedResources.DialogIds,
            new HashSet<string> { "Party1", "Party2" },
            new HashSet<string> { "Resource1", "Resource2" },
            new HashSet<string> { "Party3" },
            new HashSet<string> { "Resource1", "Resource2", "Resource3" },
            new HashSet<string> { "Party4" },
            new HashSet<string> { "Resource3" },
            new HashSet<string> { "Party5" },
            new HashSet<string> { "Resource4" }
        };

        // Act
        var (actualSql, actualParameters) = DbSetExtensions.GeneratePrefilterAuthorizedDialogsSql(authorizedResources);

        // Assert
        RemoveWhitespace(actualSql).Should().Be(RemoveWhitespace(expectedSql));
        actualParameters.Should().BeEquivalentTo(expectedParameters);
    }

    private static string RemoveWhitespace(string input) =>
        string.Concat(input.Where(c => !char.IsWhiteSpace(c)));
}
