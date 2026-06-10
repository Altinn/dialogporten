using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

[Collection(nameof(WebApiTestCollectionFixture))]
public class CreateApiOnlyDialogTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    // API-only dialogs relax the content validation rules: dialog content and/or
    // transmission content may be omitted entirely.
    [E2ETheory]
    [InlineData(false, false)] // No content at all
    [InlineData(true, true)]   // Both dialog and transmission content
    [InlineData(false, true)]  // Only transmission content
    [InlineData(true, false)]  // Only dialog content
    public async Task Should_Create_ApiOnly_Dialog_With_Relaxed_Content_Requirements(
        bool withDialogContent, bool withTransmissionContent)
    {
        // Arrange
        var dialog = DialogTestData.CreateSimpleDialog(d =>
        {
            d.IsApiOnly = true;

            if (!withDialogContent)
            {
                d.Content = null!;
            }

            d.AddTransmission(t =>
            {
                if (!withTransmissionContent)
                {
                    t.Content = null!;
                }
            });
        });

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.Created);
    }

    [E2EFact]
    public async Task Should_Reject_Regular_Dialog_Without_Title()
    {
        // Arrange
        var dialog = DialogTestData.CreateSimpleDialog(d =>
        {
            d.IsApiOnly = false;
            d.Content.Title = null!;
        });

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        response.Error.Should().NotBeNull();
        response.Error.Content.Should().NotBeNull();
        response.Error.Content.Should().Contain("title");
    }

    [E2EFact]
    public async Task Should_Reject_ApiOnly_Dialog_When_Supplied_Content_Is_Invalid()
    {
        // Arrange: if content is supplied at all, it must pass regular validation.
        var dialog = DialogTestData.CreateSimpleDialog(d =>
        {
            d.IsApiOnly = true;
            d.Content.Title = null!;
        });

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
        response.Error.Should().NotBeNull();
        response.Error.Content.Should().NotBeNull();
        response.Error.Content.Should().Contain("title");
    }

    [E2EFact]
    public async Task Should_Reject_ApiOnly_Dialog_When_Supplied_Transmission_Content_Is_Invalid()
    {
        // Arrange: an empty (non-null) transmission content must still pass regular validation.
        var dialog = DialogTestData.CreateSimpleDialog(d =>
        {
            d.IsApiOnly = true;
            d.Content = null!;
            d.AddTransmission(t => t.Content = new());
        });

        // Act
        var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            dialog,
            TestContext.Current.CancellationToken);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.BadRequest);
    }
}
