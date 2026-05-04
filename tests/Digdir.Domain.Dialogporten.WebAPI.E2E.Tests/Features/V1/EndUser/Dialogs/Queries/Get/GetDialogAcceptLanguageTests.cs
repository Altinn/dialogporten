using System.Net;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogAcceptLanguageTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_Nb_Title_When_Nb_Requested()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        // Act
        var languages = new AcceptedLanguages
        {
            AcceptedLanguage = [new AcceptedLanguage { LanguageCode = "nb", Weight = 1 }]
        };
        var response = await Fixture.EnduserApi.V1.GetDialog(dialogId, languages);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        content.Content.Title.Value.Should().HaveCount(1);
        content.Content.Title.Value.First().LanguageCode.Should().Be("nb");

        content.Content.Summary.Value.Should().HaveCount(2);

        content.Content.ExtendedStatus.Value.Should().HaveCount(1);
        content.Content.ExtendedStatus.Value.First().LanguageCode.Should().Be("nb");
    }

    [E2EFact]
    public async Task Should_Return_400_For_Invalid_Accept_Language()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        // Act
        var languages = new AcceptedLanguages
        {
            AcceptedLanguage = [new AcceptedLanguage { LanguageCode = "invalid;;;", Weight = 1 }]
        };
        var response = await Fixture.EnduserApi.V1.GetDialog(dialogId, languages);
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        response.Error!.Content.Should().Contain("Accept-Language");
    }

    [E2EFact]
    public async Task Should_Fallback_To_Nb_For_Sv()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        // Act
        var languages = new AcceptedLanguages
        {
            AcceptedLanguage = [new AcceptedLanguage { LanguageCode = "sv", Weight = 1 }]
        };
        var response = await Fixture.EnduserApi.V1.GetDialog(dialogId, languages);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        content.Content.Title.Value.Should().HaveCount(1);
        content.Content.Title.Value.First().LanguageCode.Should().Be("nb");

        content.Content.Summary.Value.Should().HaveCount(2);

        content.Content.ExtendedStatus.Value.Should().HaveCount(1);
        content.Content.ExtendedStatus.Value.First().LanguageCode.Should().Be("nb");
    }

    [E2EFact]
    public async Task Should_Fallback_To_Nb_For_Da()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        // Act
        var languages = new AcceptedLanguages
        {
            AcceptedLanguage = [new AcceptedLanguage { LanguageCode = "da", Weight = 1 }]
        };
        var response = await Fixture.EnduserApi.V1.GetDialog(dialogId, languages);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        content.Content.Title.Value.Should().HaveCount(1);
        content.Content.Title.Value.First().LanguageCode.Should().Be("nb");

        content.Content.Summary.Value.Should().HaveCount(2);

        content.Content.ExtendedStatus.Value.Should().HaveCount(1);
        content.Content.ExtendedStatus.Value.First().LanguageCode.Should().Be("nb");
    }

    [E2EFact]
    public async Task Should_Return_First_Available_Language_For_Wildcard()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        // Act
        var languages = new AcceptedLanguages
        {
            AcceptedLanguage = [new AcceptedLanguage { LanguageCode = "*", Weight = 1 }]
        };
        var response = await Fixture.EnduserApi.V1.GetDialog(dialogId, languages);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        content.Content.Title.Value.Should().HaveCount(1);
        content.Content.Title.Value.First().LanguageCode.Should().Be("en");

        content.Content.Summary.Value.Should().HaveCount(2);

        content.Content.ExtendedStatus.Value.Should().HaveCount(1);
        content.Content.ExtendedStatus.Value.First().LanguageCode.Should().Be("nb");
    }

    [E2EFact]
    public async Task Should_Return_Exact_Match_For_It_And_Fallback_For_Others()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        // Act
        var languages = new AcceptedLanguages
        {
            AcceptedLanguage = [new AcceptedLanguage { LanguageCode = "it", Weight = 1 }]
        };
        var response = await Fixture.EnduserApi.V1.GetDialog(dialogId, languages);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        content.Content.Title.Value.Should().HaveCount(1);
        content.Content.Title.Value.First().LanguageCode.Should().Be("en");

        content.Content.Summary.Value.Should().HaveCount(1);
        content.Content.Summary.Value.First().LanguageCode.Should().Be("it");

        content.Content.ExtendedStatus.Value.Should().HaveCount(1);
        content.Content.ExtendedStatus.Value.First().LanguageCode.Should().Be("nb");
    }

    [E2EFact]
    public async Task Should_Return_All_Localizations_When_Empty_AcceptedLanguages()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        // Act — the SDK sends an empty Accept-Language header rather than omitting it
        var response = await Fixture.EnduserApi.V1.GetDialog(
            dialogId,
            new AcceptedLanguages());

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

        content.Content.Title.Value.Should().HaveCount(2);
        content.Content.Summary.Value.Should().HaveCount(2);
        content.Content.ExtendedStatus.Value.Should().HaveCount(1);
    }

    private async Task<Guid> CreateDialogWithMultilingualContent() =>
        await Fixture.ServiceownerApi.CreateComplexDialogAsync(modify: d =>
        {
            d.Content.Title = DialogTestData.CreateContentValue(
                value:
                [
                    DialogTestData.CreateLocalization("en-title-content", "en"),
                    DialogTestData.CreateLocalization("nb-title-content")
                ]);
            d.Content.ExtendedStatus = DialogTestData.CreateContentValue(
                value: [DialogTestData.CreateLocalization("nb-Status-content")]);
            d.Content.Summary = DialogTestData.CreateContentValue(
                value:
                [
                    DialogTestData.CreateLocalization("it-summary-content", "it"),
                    DialogTestData.CreateLocalization("fr-summary-content", "fr")
                ]);
        });
}
