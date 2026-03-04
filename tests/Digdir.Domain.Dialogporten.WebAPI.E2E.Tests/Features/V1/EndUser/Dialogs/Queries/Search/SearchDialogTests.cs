using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchDialogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Not_See_Sensitive_Content_With_Inadequate_Auth_Level()
    {
        // Arrange
        const string sensitiveTitle = "Sensitive title!";
        const string nonSensitiveTitle = "Non-sensitive title!";

        const string sensitiveSummary = "Sensitive summary!";
        const string nonSensitiveSummary = "Non-sensitive summary!";

        var sentinelLabel = Guid.NewGuid().ToString();
        // var sentinelLabel = Guid.Parse("51a1fa16-e69c-478d-bb02-acd44146049e");
        // var dialogId = Guid.Parse("019cbac0-2617-76b2-9c6b-decd9566c8cd");
        var dialogId = await Fixture.ServiceownerApi.CreateComplexDialogAsync(x =>
        {
            // x.Id = dialogId;
            // This serviceResource requires auth level 4
            x.ServiceResource = "urn:altinn:resource:ttd-dialogporten-transmissions-test";

            x.Content.Title = GetContentValue(sensitiveTitle);
            x.Content.Summary = GetContentValue(sensitiveSummary);

            x.Content.NonSensitiveTitle = GetContentValue(nonSensitiveTitle);
            x.Content.NonSensitiveSummary = GetContentValue(nonSensitiveSummary);

            x.SearchTags = [new() { Value = sentinelLabel }];
        });


        // var dialogResult = await Fixture.EnduserApi.V1EndUserDialogsQueriesGetDialog(dialogId, new(), TestContext.Current.CancellationToken);
        // Console.WriteLine(dialogResult);
        await Task.Delay(10000);
        var searchResult = await Fixture.EnduserApi.V1EndUserDialogsQueriesSearchDialog(new()
        {
            Party = [E2EConstants.DefaultParty],
            Search = sentinelLabel
        }, new(), TestContext.Current.CancellationToken);

        searchResult.Content!.Items.Should().NotBeNull();
        searchResult.Content!.Items.Should()
            .ContainSingle(x => x.Id == dialogId);
        Console.WriteLine(searchResult);
    }

    private static Altinn.ApiClients.Dialogporten.Features.V1.V1CommonContent_ContentValue GetContentValue(string value) => new()
    {
        Value = [new() { Value = value, LanguageCode = "en" }]
    };
}
