using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common.Authorization;

public class AuthorizationCheckBuilderTests
{
    private const string DialogParty = "urn:altinn:organization:identifier-no:713330310";
    private const string OtherParty = "urn:altinn:organization:identifier-no:991825827";

    // Ported from the legacy DialogEntityExtensionsTests, with one deliberate change: legacy
    // authorization attributes always derive the "read" action, regardless of attribute type.
    // (Previously, subresource/task attributes derived "transmissionread" — an action no policy
    // in any environment ever implemented.)
    [Fact]
    public void GetAuthorizationChecksShouldReturnCorrectChecksForLegacyAuthorizationAttributes()
    {
        // Arrange
        var dialogEntity = new DialogEntity
        {
            Party = DialogParty,
            ApiActions = [
                new DialogApiAction { Action = "read" },
                new DialogApiAction { Action = "read" },
                new DialogApiAction { Action = "read", AuthorizationAttribute = "foo" },
                new DialogApiAction { Action = "transmissionread", AuthorizationAttribute = "bar" },
                new DialogApiAction { Action = "apiread" },
            ],
            GuiActions = [
                new DialogGuiAction { Action = "read" },
                new DialogGuiAction { Action = "read" },
                new DialogGuiAction { Action = "read", AuthorizationAttribute = "foo" },
                new DialogGuiAction { Action = "transmissionread", AuthorizationAttribute = "bar" },
                new DialogGuiAction { Action = "guiread" },
            ],
            Transmissions =
            [
                new() { AuthorizationAttribute = "bar" },
                new() { AuthorizationAttribute = "urn:altinn:subresource:bar" },
                new() { AuthorizationAttribute = "urn:altinn:task:Task_1" },
                new() { AuthorizationAttribute = "urn:altinn:resource:some-service:element1" },
                new() { AuthorizationAttribute = "urn:altinn:resource:app_ttd_some-app" }
            ]
        };

        // Act
        var checks = dialogEntity.GetAuthorizationChecks();

        // Assert
        Assert.NotNull(checks);
        Assert.Equal(10, checks.Count);
        Assert.All(checks, c => Assert.Equal([DialogParty], c.Parties));
        Assert.Contains(checks, c => c is { Action: Constants.ReadAction, Resource.Kind: AuthorizationResourceSpecKind.Main });
        Assert.Contains(checks, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "foo" });
        // Explicit legacy actions on api/gui actions are preserved verbatim...
        Assert.Contains(checks, c => c is { Action: "transmissionread", Resource.LegacyAuthorizationAttribute: "bar" });
        Assert.Contains(checks, c => c is { Action: "apiread", Resource.Kind: AuthorizationResourceSpecKind.Main });
        Assert.Contains(checks, c => c is { Action: "guiread", Resource.Kind: AuthorizationResourceSpecKind.Main });
        // ...while transmissions always derive "read", whatever the attribute type
        Assert.Contains(checks, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "bar" });
        Assert.Contains(checks, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "urn:altinn:subresource:bar" });
        Assert.Contains(checks, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "urn:altinn:task:Task_1" });
        Assert.Contains(checks, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "urn:altinn:resource:some-service:element1" });
        Assert.Contains(checks, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "urn:altinn:resource:app_ttd_some-app" });
    }

    [Fact]
    public void GetAuthorizationChecksShouldAlwaysIncludeMainResourceReadCheck()
    {
        var dialogEntity = new DialogEntity { Party = DialogParty };

        var checks = dialogEntity.GetAuthorizationChecks();

        var check = Assert.Single(checks);
        Assert.Equal(dialogEntity.GetMainResourceReadCheck(), check);
        Assert.Equal(Constants.ReadAction, check.Action);
        Assert.Equal(AuthorizationResourceSpecKind.Main, check.Resource.Kind);
        Assert.Equal([DialogParty], check.Parties);
    }

    [Fact]
    public void GetAuthorizationChecksShouldBuildContextChecksForAllCarriers()
    {
        // Arrange
        var dialogEntity = new DialogEntity
        {
            Party = DialogParty,
            ApiActions =
            [
                new DialogApiAction
                {
                    AuthorizationContext = new DialogApiActionAuthorizationContext
                    {
                        Action = "write",
                        ServiceResource = "urn:altinn:resource:other-service",
                        Parties = [OtherParty]
                    }
                }
            ],
            GuiActions =
            [
                new DialogGuiAction
                {
                    AuthorizationContext = new DialogGuiActionAuthorizationContext
                    {
                        Action = "sign",
                        AdditionalResourceAttribute = "urn:altinn:task:Task_1",
                        Parties = [OtherParty],
                        IncludeDialogParty = true
                    }
                }
            ],
            Attachments =
            [
                new DialogAttachment
                {
                    AuthorizationContext = new AttachmentAuthorizationContext
                    {
                        // Attachments honor an explicit action override like any other carrier
                        Action = "attachmentread",
                        AdditionalResourceAttribute = "urn:altinn:subresource:secret",
                        Parties = [OtherParty]
                    }
                }
            ],
            Transmissions =
            [
                new DialogTransmission
                {
                    AuthorizationContext = new DialogTransmissionAuthorizationContext
                    {
                        ServiceResource = "urn:altinn:resource:other-service",
                        Parties = [OtherParty]
                    },
                    Attachments =
                    [
                        new DialogTransmissionAttachment
                        {
                            AuthorizationContext = new AttachmentAuthorizationContext
                            {
                                AdditionalResourceAttribute = "urn:altinn:task:Task_2",
                                Parties = [OtherParty]
                            }
                        }
                    ],
                    NavigationalActions =
                    [
                        new DialogTransmissionNavigationalAction
                        {
                            AuthorizationContext = new DialogTransmissionNavigationalActionAuthorizationContext
                            {
                                ServiceResource = "urn:altinn:resource:third-service",
                                Parties = [OtherParty]
                            }
                        }
                    ]
                }
            ]
        };

        // Act
        var checks = dialogEntity.GetAuthorizationChecks();

        // Assert: 6 context checks + the main read check
        Assert.Equal(7, checks.Count);

        // Api action: explicit action, context parties only
        Assert.Contains(checks, c => c is
        {
            Action: "write",
            Resource: { Kind: AuthorizationResourceSpecKind.Context, ServiceResource: "urn:altinn:resource:other-service" }
        } && c.Parties.SequenceEqual([OtherParty]));

        // Gui action: includeDialogParty merges the dialog party into the (sorted) party set
        var guiCheck = Assert.Single(checks, c => c.Action == "sign");
        Assert.Equal(AuthorizationResourceSpecKind.Context, guiCheck.Resource.Kind);
        Assert.Equal("urn:altinn:task:Task_1", guiCheck.Resource.AdditionalResourceAttribute);
        Assert.Equal(new[] { DialogParty, OtherParty }.Order(StringComparer.Ordinal), guiCheck.Parties);

        // Dialog attachment: explicit action override is honored
        Assert.Contains(checks, c => c is
        {
            Action: "attachmentread",
            Resource.AdditionalResourceAttribute: "urn:altinn:subresource:secret"
        });

        // Transmission: no explicit action => defaults to read
        Assert.Contains(checks, c => c is
        {
            Action: Constants.ReadAction,
            Resource: { Kind: AuthorizationResourceSpecKind.Context, ServiceResource: "urn:altinn:resource:other-service" }
        });

        // Transmission attachment: no explicit action => defaults to read
        Assert.Contains(checks, c => c is
        {
            Action: Constants.ReadAction,
            Resource.AdditionalResourceAttribute: "urn:altinn:task:Task_2"
        });

        // Navigational action: no explicit action => defaults to read
        Assert.Contains(checks, c => c is
        {
            Action: Constants.ReadAction,
            Resource.ServiceResource: "urn:altinn:resource:third-service"
        });
    }

    [Theory]
    [InlineData("urn:altinn:resource:other-service", null)]
    [InlineData(null, "urn:altinn:task:Task_1")]
    [InlineData(null, null)]
    public void TransmissionContextWithoutActionShouldDefaultToRead(
        string? serviceResource, string? additionalResourceAttribute)
    {
        var transmission = new DialogTransmission
        {
            AuthorizationContext = new DialogTransmissionAuthorizationContext
            {
                ServiceResource = serviceResource,
                AdditionalResourceAttribute = additionalResourceAttribute,
                Parties = [OtherParty]
            }
        };

        var check = transmission.GetAuthorizationCheck(new DialogEntity { Party = DialogParty });

        Assert.NotNull(check);
        Assert.Equal(Constants.ReadAction, check.Action);
    }

    [Fact]
    public void TransmissionContextWithExplicitActionShouldUseIt()
    {
        var transmission = new DialogTransmission
        {
            AuthorizationContext = new DialogTransmissionAuthorizationContext
            {
                Action = "customread",
                AdditionalResourceAttribute = "urn:altinn:task:Task_1",
                Parties = [OtherParty]
            }
        };

        var check = transmission.GetAuthorizationCheck(new DialogEntity { Party = DialogParty });

        Assert.NotNull(check);
        Assert.Equal("customread", check.Action);
    }

    [Fact]
    public void ContextWithEmptyPartiesAndNoDialogPartyShouldProduceCheckWithNoParties()
    {
        // Write-side validation rejects this shape; if it ever occurs anyway, the check must
        // fail closed (no parties => no evaluation units => never authorized), never silently
        // fall back to the dialog party.
        var transmission = new DialogTransmission
        {
            AuthorizationContext = new DialogTransmissionAuthorizationContext
            {
                ServiceResource = "urn:altinn:resource:other-service",
                Parties = [],
                IncludeDialogParty = false
            }
        };

        var check = transmission.GetAuthorizationCheck(new DialogEntity { Party = DialogParty });

        Assert.NotNull(check);
        Assert.Empty(check.Parties);
    }

    [Fact]
    public void GetAuthorizationChecksShouldDeduplicateEqualChecks()
    {
        var dialogEntity = new DialogEntity
        {
            Party = DialogParty,
            Attachments =
            [
                new DialogAttachment
                {
                    AuthorizationContext = new AttachmentAuthorizationContext
                    {
                        ServiceResource = "urn:altinn:resource:other-service",
                        Parties = [OtherParty, DialogParty]
                    }
                }
            ],
            Transmissions =
            [
                new DialogTransmission
                {
                    AuthorizationContext = new DialogTransmissionAuthorizationContext
                    {
                        ServiceResource = "urn:altinn:resource:other-service",
                        // Same effective (sorted, distinct) party set as the attachment above
                        Parties = [DialogParty, OtherParty, OtherParty]
                    }
                }
            ]
        };

        var checks = dialogEntity.GetAuthorizationChecks();

        // One deduplicated context check + the main read check
        Assert.Equal(2, checks.Count);
    }

    [Fact]
    public void ContextChecksWithMainResourceShapeMustNotSatisfyMainResourcePredicates()
    {
        // A context with no resource fields evaluates against the dialog's own resource, but it must
        // never satisfy the main-resource predicates (which gate dialog-level access): a foreign-party
        // context grant must not make the dialog itself accessible.
        var contextCheck = new AuthorizationCheck(
            Constants.ReadAction,
            AuthorizationResourceSpec.FromContext(null, null),
            [OtherParty]);

        var result = new DialogDetailsAuthorizationResult
        {
            AuthorizedChecks = [AuthorizedCheck.FullyPermitted(contextCheck)]
        };

        Assert.True(result.HasAccess(contextCheck));
        Assert.False(result.HasAccessToMainResource());
        Assert.False(result.HasReadAccessToMainResource());
    }
}
