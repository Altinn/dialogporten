using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogTests : ApplicationCollectionFixture
{
    public SearchDialogTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task SearchWithFreetextRequiresEndUserId_OK()
    {
        // Arrange
        var searchDialogQuery = new SearchDialogQuery
        {
            Search = "foobar",
            Party = [DialogGenerator.GenerateRandomParty()],
            EndUserId = DialogGenerator.GenerateRandomParty(forcePerson: true)
        };

        // Act
        var response = await Application.Send(searchDialogQuery);

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchWithFreetextRequiresEndUserId_MissingShouldFail()
    {
        // Arrange
        var searchDialogQuery = new SearchDialogQuery
        {
            Search = "foobar"
        };

        // Act
        var response = await Application.Send(searchDialogQuery);

        // Assert
        response.TryPickT1(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
    }
}
