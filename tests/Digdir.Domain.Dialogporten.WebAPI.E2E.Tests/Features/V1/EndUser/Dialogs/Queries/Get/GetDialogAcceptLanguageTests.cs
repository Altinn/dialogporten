using System.Net;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogAcceptLanguageTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_Nb_Title_When_Nb_Requested()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        try
        {
            // Act
            var languages = new V1EndUserCommon_AcceptedLanguages
            {
                AcceptedLanguage = [new V1EndUserCommon_AcceptedLanguage { LanguageCode = "nb", Weight = 1 }]
            };
            var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

            // Assert
            response.IsSuccessful.Should().BeTrue();
            var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

            content.Content.Title.Value.Should().HaveCount(1);
            content.Content.Title.Value[0].LanguageCode.Should().Be("nb");

            content.Content.Summary.Value.Should().HaveCount(2);

            content.Content.ExtendedStatus!.Value.Should().HaveCount(1);
            content.Content.ExtendedStatus.Value[0].LanguageCode.Should().Be("nb");
        }
        finally
        {
            // Cleanup
            await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match: null);
        }
    }

    [E2EFact]
    public async Task Should_Return_400_For_Invalid_Accept_Language()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        try
        {
            // Act - Use raw HttpClient to send invalid Accept-Language header
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/enduser/dialogs/{dialogId}");
            request.Headers.Add("Accept-Language", "it;a=1.0, nb");
            var response = await Fixture.EnduserHttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
        finally
        {
            // Cleanup
            await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match: null);
        }
    }

    [E2EFact]
    public async Task Should_Fallback_To_Nb_For_Sv()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        try
        {
            // Act
            var languages = new V1EndUserCommon_AcceptedLanguages
            {
                AcceptedLanguage = [new V1EndUserCommon_AcceptedLanguage { LanguageCode = "sv", Weight = 1 }]
            };
            var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

            // Assert
            response.IsSuccessful.Should().BeTrue();
            var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

            content.Content.Title.Value.Should().HaveCount(1);
            content.Content.Title.Value[0].LanguageCode.Should().Be("nb");

            content.Content.Summary.Value.Should().HaveCount(2);

            content.Content.ExtendedStatus!.Value.Should().HaveCount(1);
            content.Content.ExtendedStatus.Value[0].LanguageCode.Should().Be("nb");
        }
        finally
        {
            // Cleanup
            await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match: null);
        }
    }

    [E2EFact]
    public async Task Should_Fallback_To_Nb_For_Da()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        try
        {
            // Act
            var languages = new V1EndUserCommon_AcceptedLanguages
            {
                AcceptedLanguage = [new V1EndUserCommon_AcceptedLanguage { LanguageCode = "da", Weight = 1 }]
            };
            var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

            // Assert
            response.IsSuccessful.Should().BeTrue();
            var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

            content.Content.Title.Value.Should().HaveCount(1);
            content.Content.Title.Value[0].LanguageCode.Should().Be("nb");

            content.Content.Summary.Value.Should().HaveCount(2);

            content.Content.ExtendedStatus!.Value.Should().HaveCount(1);
            content.Content.ExtendedStatus.Value[0].LanguageCode.Should().Be("nb");
        }
        finally
        {
            // Cleanup
            await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match: null);
        }
    }

    [E2EFact]
    public async Task Should_Return_First_Available_Language_For_Wildcard()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        try
        {
            // Act
            var languages = new V1EndUserCommon_AcceptedLanguages
            {
                AcceptedLanguage = [new V1EndUserCommon_AcceptedLanguage { LanguageCode = "*", Weight = 1 }]
            };
            var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

            // Assert
            response.IsSuccessful.Should().BeTrue();
            var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

            content.Content.Title.Value.Should().HaveCount(1);
            content.Content.Title.Value[0].LanguageCode.Should().Be("en");

            content.Content.Summary.Value.Should().HaveCount(2);

            content.Content.ExtendedStatus!.Value.Should().HaveCount(1);
            content.Content.ExtendedStatus.Value[0].LanguageCode.Should().Be("nb");
        }
        finally
        {
            // Cleanup
            await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match: null);
        }
    }

    [E2EFact]
    public async Task Should_Return_Exact_Match_For_It_And_Fallback_For_Others()
    {
        // Arrange
        var dialogId = await CreateDialogWithMultilingualContent();

        try
        {
            // Act
            var languages = new V1EndUserCommon_AcceptedLanguages
            {
                AcceptedLanguage = [new V1EndUserCommon_AcceptedLanguage { LanguageCode = "it", Weight = 1 }]
            };
            var response = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, languages);

            // Assert
            response.IsSuccessful.Should().BeTrue();
            var content = response.Content ?? throw new InvalidOperationException("Dialog content was null.");

            content.Content.Title.Value.Should().HaveCount(1);
            content.Content.Title.Value[0].LanguageCode.Should().Be("en");

            content.Content.Summary.Value.Should().HaveCount(1);
            content.Content.Summary.Value[0].LanguageCode.Should().Be("it");

            content.Content.ExtendedStatus!.Value.Should().HaveCount(1);
            content.Content.ExtendedStatus.Value[0].LanguageCode.Should().Be("nb");
        }
        finally
        {
            // Cleanup
            await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsPurgeDialog(dialogId, if_Match: null);
        }
    }

    /// <summary>
    /// Shared helper method to create a dialog with multilingual content for all tests
    /// </summary>
    private async Task<Guid> CreateDialogWithMultilingualContent()
    {
        return await Fixture.ServiceownerApi.CreateComplexDialogAsync(modify: d =>
        {
            d.VisibleFrom = null;
            d.Content!.Title = DialogTestData.CreateContentValue(
                value: [
                    DialogTestData.CreateLocalization("en-title-content", "en"),
                    DialogTestData.CreateLocalization("nb-title-content", "nb")
                ]);
            d.Content!.ExtendedStatus = DialogTestData.CreateContentValue(
                value: [DialogTestData.CreateLocalization("nb-Status-content", "nb")]);
            d.Content!.Summary = DialogTestData.CreateContentValue(
                value: [
                    DialogTestData.CreateLocalization("it-summary-content", "it"),
                    DialogTestData.CreateLocalization("fr-summary-content", "fr")
                ]);
        });
    }
}
