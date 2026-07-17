using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.Metadata.ServiceResources.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetServiceResourceMetadataSnapshotTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2ETheory]
    [InlineData("nb")]
    [InlineData("nn")]
    [InlineData("en")]
    public async Task Get_ServiceResourceMetadata_Verify_Language_Codes(string expectedLanguageCode)
    {
        // Arrange
        var noLanguage = new V1EndUserCommon_AcceptedLanguages();
        var resNoLanguage = await Fixture.MetadataApi.V1MetadataServiceResourcesGetServiceResourceMetadata(noLanguage);

        resNoLanguage.ShouldHaveStatusCode(HttpStatusCode.OK);
        resNoLanguage.Content.Should().NotBeNull();

        var languages = new V1EndUserCommon_AcceptedLanguages
        {
            AcceptedLanguage = [new V1EndUserCommon_AcceptedLanguage
                {
                    LanguageCode = expectedLanguageCode,
                    Weight = 1
                }
            ]
        };
        var resWithLanguage = await Fixture.MetadataApi.V1MetadataServiceResourcesGetServiceResourceMetadata(languages);

        // Assert
        resWithLanguage.ShouldHaveStatusCode(HttpStatusCode.OK);
        resWithLanguage.Content.Should().NotBeNull();
        var localizedItemByResourceId = resWithLanguage.Content.Items
            .GroupBy(x => x.ServiceResource.Id)
            .ToDictionary(k => k.Key, v => v.Select(y => y));

        resNoLanguage.Content.Items.Should().AllSatisfy(item =>
        {
            var resourceId = item.ServiceResource.Id;
            var localizedItem = localizedItemByResourceId[resourceId].Single();
            var localizedPackages = localizedItem.AccessPackages
                .GroupBy(x => x.Urn)
                .ToDictionary(k => k.Key, v => v.Select(y => y));
            var localizedRoles = localizedItem.Roles
                .GroupBy(x => x.Urn)
                .ToDictionary(k => k.Key, v => v.Select(y => y));

            AssertLanguageCodes(item.ServiceResource.Name, localizedItem.ServiceResource.Name, expectedLanguageCode);
            AssertLanguageCodes(item.ServiceOwner.Name, localizedItem.ServiceOwner.Name, expectedLanguageCode);

            item.AccessPackages.Should().AllSatisfy(accessPackage =>
            {
                var localizedPackage = localizedPackages[accessPackage.Urn].Single();
                AssertLanguageCodes(accessPackage.Name, localizedPackage.Name, expectedLanguageCode);
            });
            item.Roles.Should().AllSatisfy(role =>
            {
                var localizedRole = localizedRoles[role.Urn].Single();
                AssertLanguageCodes(role.Name, localizedRole.Name, expectedLanguageCode);
            });
        });
    }

    private static void AssertLanguageCodes(
        ICollection<V1CommonLocalizations_Localization> allLocalizations,
        ICollection<V1CommonLocalizations_Localization> localizedLocalizations,
        string expectedLangCode)
    {
        if (allLocalizations.Select(n => n.LanguageCode).Contains(expectedLangCode))
        {
            localizedLocalizations.Single().LanguageCode.Should().Be(expectedLangCode);
        }
    }
}
