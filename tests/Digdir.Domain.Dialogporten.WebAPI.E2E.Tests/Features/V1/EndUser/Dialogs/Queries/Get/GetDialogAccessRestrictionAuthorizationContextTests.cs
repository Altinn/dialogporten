using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Extensions;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

/// <summary>
/// Dialog party checks with access restrictions, evaluated by the real PDP. These require a PDP supporting the
/// "urn:altinn:authorization:access-restriction" attribute (altinn-auth#4292). A PDP without that support ignores
/// the attribute and evaluates every check as unrestricted, which the fail-open guard below detects.
/// </summary>
[Collection(nameof(WebApiTestCollectionFixture))]
public class GetDialogAccessRestrictionAuthorizationContextTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    // An organization the default end user holds no client delegation through. Must exist in the register of the
    // environment under test, as the PDP looks up the via-party.
    private const string ViaParty = "urn:altinn:organization:identifier-no:991825827";

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Deny_Client_Delegation_Check_For_End_User_With_Only_Direct_Access()
    {
        // Arrange: the default end user reads their own dialog on the dialog's own resource. The unrestricted
        // check is the control; the client delegation check must be denied, as the end user's access does not
        // come from a client delegation through ViaParty. If it is permitted, the PDP ignored the restriction.
        var ids = OrderedVersion7Ids(2);
        var unrestrictedId = ids[0];
        var clientDelegationId = ids[1];

        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
            dialog.Transmissions =
            [
                CreateTransmission(unrestrictedId, new()
                {
                    AccessRestriction = DialogsEntitiesAuthorizationContexts_DialogPartyCheckAccessRestriction.None
                }),
                CreateTransmission(clientDelegationId, new()
                {
                    AccessRestriction = DialogsEntitiesAuthorizationContexts_DialogPartyCheckAccessRestriction.ClientDelegation,
                    ViaParty = ViaParty
                })
            ]);

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Transmissions.Single(x => x.Id == unrestrictedId).IsAuthorized.Should().BeTrue();
        response.Content.Transmissions.Single(x => x.Id == clientDelegationId).IsAuthorized.Should().BeFalse(
            "the end user has no client delegation through the via party; a permit means the PDP ignored the access restriction");
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Permit_Direct_And_Hierarchy_Check_For_End_User_With_Direct_Access()
    {
        // Arrange: the default end user's access to their own dialog does not pass through another organization.
        var transmissionId = Guid.CreateVersion7();
        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
            dialog.Transmissions =
            [
                CreateTransmission(transmissionId, new()
                {
                    AccessRestriction = DialogsEntitiesAuthorizationContexts_DialogPartyCheckAccessRestriction.DirectAndHierarchy
                })
            ]);

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Transmissions.Single(x => x.Id == transmissionId).IsAuthorized.Should().BeTrue();
    }

    [E2EFact(SkipOnEnvironments = ["yt01"])]
    public async Task Should_Permit_Client_Delegation_Check_Only_Through_The_Delegating_Firm()
    {
        // Arrange: the agent's only access to the client comes from a client delegation through the firm (see
        // E2EConstants.ClientDelegationFirmOrgNo). Only the check naming that firm may permit; naming another firm,
        // or requiring access that does not pass through another organization, must deny.
        var ids = OrderedVersion7Ids(3);
        var throughFirmId = ids[0];
        var throughOtherFirmId = ids[1];
        var directAndHierarchyId = ids[2];

        var dialogId = await Fixture.ServiceownerApi.CreateSimpleDialogAsync(dialog =>
        {
            dialog.Party = E2EConstants.ClientDelegationClientOrgUrn;
            dialog.Transmissions =
            [
                CreateTransmission(throughFirmId, new()
                {
                    AccessRestriction = DialogsEntitiesAuthorizationContexts_DialogPartyCheckAccessRestriction.ClientDelegation,
                    ViaParty = E2EConstants.ClientDelegationFirmOrgUrn
                }),
                CreateTransmission(throughOtherFirmId, new()
                {
                    AccessRestriction = DialogsEntitiesAuthorizationContexts_DialogPartyCheckAccessRestriction.ClientDelegation,
                    ViaParty = E2EConstants.ClientDelegationOtherFirmOrgUrn
                }),
                CreateTransmission(directAndHierarchyId, new()
                {
                    AccessRestriction = DialogsEntitiesAuthorizationContexts_DialogPartyCheckAccessRestriction.DirectAndHierarchy
                })
            ];
        });

        using var _ = Fixture.UseEndUserTokenOverrides(ssn: E2EConstants.ClientDelegationAgentSsn);

        // Act
        var response = await Fixture.EndUserApi.GetDialog(dialogId);

        // Assert
        response.ShouldHaveStatusCode(HttpStatusCode.OK);
        response.Content.Should().NotBeNull();
        response.Content.Transmissions.Single(x => x.Id == throughFirmId).IsAuthorized.Should().BeTrue(
            "the agent's access to the client is client delegated through this firm");
        response.Content.Transmissions.Single(x => x.Id == throughOtherFirmId).IsAuthorized.Should().BeFalse(
            "the agent has no client delegation through the other firm; a permit means the PDP ignored the via party");
        response.Content.Transmissions.Single(x => x.Id == directAndHierarchyId).IsAuthorized.Should().BeFalse(
            "the agent's only access to the client is a client delegation; a permit means the PDP ignored the access restriction");
    }

    private static V1ServiceOwnerDialogsCommandsCreate_Transmission CreateTransmission(
        Guid id, V1CommonAuthorizationContexts_DialogPartyCheck dialogPartyCheck) =>
        new()
        {
            Id = id,
            Type = DialogsEntitiesTransmissions_DialogTransmissionType.Information,
            Sender = new V1ServiceOwnerCommonActors_Actor { ActorType = Actors_ActorType.ServiceOwner },
            AuthorizationContext = new V1CommonAuthorizationContexts_AuthorizationContext
            {
                Parties = [],
                DialogPartyChecks = [dialogPartyCheck],
                UnauthorizedPresentation = DialogsEntitiesAuthorizationContexts_AuthorizationContextUnauthorizedPresentation.Disabled
            },
            Content = new V1ServiceOwnerDialogsCommandsCreate_TransmissionContent
            {
                Title = DialogTestData.CreateContentValue("Tittel", "nb"),
                Summary = DialogTestData.CreateContentValue("Sammendrag", "nb")
            }
        };

    /// <summary>
    /// UUIDv7 ids in ascending order, for collections whose response order is decided by Id.
    /// </summary>
    private static Guid[] OrderedVersion7Ids(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => Guid.CreateVersion7())
            .Order()
            .ToArray();
}
