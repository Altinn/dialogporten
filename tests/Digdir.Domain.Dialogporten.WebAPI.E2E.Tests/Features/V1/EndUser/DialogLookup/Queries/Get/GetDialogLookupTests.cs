using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.DialogLookup.Queries.Get;

[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogLookupTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Return_404_For_Unknown_InstanceRef()
    {
        // Arrange
        var instanceRef = $"urn:altinn:instance-id:1337/{Guid.NewGuid()}";

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogLookupQueriesGetDialogLookup(
            instanceRef,
            new(),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.NotFound);
    }

    [E2EFact]
    public async Task Should_Return_200_For_Existing_InstanceRef()
    {
        // Arrange
        var instanceGuid = Guid.NewGuid();
        var instanceRef = $"urn:altinn:instance-id:1337/{instanceGuid}";
        var party = string.Empty;
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            party = dialog.Party;
            dialog.ServiceOwnerContext = new V1ServiceOwnerDialogsCommandsCreate_DialogServiceOwnerContext
            {
                ServiceOwnerLabels =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_ServiceOwnerLabel
                    {
                        Value = $"urn:altinn:integration:storage:1337/{instanceGuid}"
                    }
                ]
            };
        });

        // Act
        var response = await Fixture.EnduserApi.V1EndUserDialogLookupQueriesGetDialogLookup(
            instanceRef,
            new(),
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();

        var lookup = response.Content;
        lookup.DialogId.Should().Be(dialogId);
        lookup.InstanceRef.Should().Be(instanceRef.ToLowerInvariant());
        lookup.Party.Should().NotBeEmpty().And.Be(party);
        lookup.ServiceResource.Id.Should().NotBeNullOrWhiteSpace();
        lookup.ServiceResource.MinimumAuthenticationLevel.Should().Be(2);
        lookup.ServiceOwner.Code.Should().NotBeNullOrWhiteSpace();
    }
}
