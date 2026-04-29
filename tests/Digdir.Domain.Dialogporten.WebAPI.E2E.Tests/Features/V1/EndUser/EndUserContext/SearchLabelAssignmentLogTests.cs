using System.Net;
using Altinn.ApiClients.Dialogporten.EndUser.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using static Altinn.ApiClients.Dialogporten.EndUser.Features.V1.DialogEndUserContextsEntities_SystemLabel;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.EndUserContext;

[Collection(nameof(WebApiTestCollectionFixture))]
public class SearchLabelAssignmentLogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    [E2EFact]
    public async Task Should_Get_Label_Assignment_Log()
    {
        // Arrange
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync();

        var setLabelResponse = await Fixture.EnduserApi.SetSystemLabels(
            dialogId,
            request => request.AddLabels = [Bin]);

        setLabelResponse.ShouldHaveStatusCode(HttpStatusCode.NoContent);

        // Act
        var response = await Fixture.EnduserApi.GetSystemLabelAssignmentLog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        var content = response.Content ?? throw new InvalidOperationException("Label assignment log content was null.");
        content.Should().ContainSingle().Which.Action.Should().Be("set");
    }
}
