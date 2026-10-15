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
        Assert.All(checks, c => Assert.Equal([DialogParty], c.Units.Select(u => u.Party)));
        Assert.All(checks, c => Assert.Equal(AuthorizationAccessRestriction.None, Assert.Single(c.Units).AccessRestriction));
        var units = checks.SelectMany(c => c.Units).ToList();
        Assert.Contains(units, c => c is { Action: Constants.ReadAction, Resource.Kind: AuthorizationResourceSpecKind.Main });
        Assert.Contains(units, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "foo" });
        // Explicit legacy actions on api/gui actions are preserved verbatim...
        Assert.Contains(units, c => c is { Action: "transmissionread", Resource.LegacyAuthorizationAttribute: "bar" });
        Assert.Contains(units, c => c is { Action: "apiread", Resource.Kind: AuthorizationResourceSpecKind.Main });
        Assert.Contains(units, c => c is { Action: "guiread", Resource.Kind: AuthorizationResourceSpecKind.Main });
        // ...while transmissions always derive "read", whatever the attribute type
        Assert.Contains(units, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "bar" });
        Assert.Contains(units, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "urn:altinn:subresource:bar" });
        Assert.Contains(units, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "urn:altinn:task:Task_1" });
        Assert.Contains(units, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "urn:altinn:resource:some-service:element1" });
        Assert.Contains(units, c => c is { Action: Constants.ReadAction, Resource.LegacyAuthorizationAttribute: "urn:altinn:resource:app_ttd_some-app" });
    }

    [Fact]
    public void GetAuthorizationChecksShouldAlwaysIncludeMainResourceReadCheck()
    {
        var dialogEntity = new DialogEntity { Party = DialogParty };

        var checks = dialogEntity.GetAuthorizationChecks();

        var check = Assert.Single(checks);
        Assert.Equal(dialogEntity.GetMainResourceReadCheck(), check);
        var unit = Assert.Single(check.Units);
        Assert.Equal(Constants.ReadAction, unit.Action);
        Assert.Equal(AuthorizationResourceSpecKind.Main, unit.Resource.Kind);
        Assert.Equal(DialogParty, unit.Party);
        Assert.Equal(AuthorizationAccessRestriction.None, unit.AccessRestriction);
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
                        DialogPartyChecks = [new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.None }]
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
        Assert.Contains(checks, c => c.Units is [
        {
            Action: "write",
            Resource: { Kind: AuthorizationResourceSpecKind.Context, ServiceResource: "urn:altinn:resource:other-service" },
            Party: OtherParty
        }]);

        // Gui action: an unrestricted dialog party check adds the dialog party with the context's right
        var guiCheck = Assert.Single(checks, c => c.Units.Any(u => u.Action == "sign"));
        Assert.All(guiCheck.Units, u =>
        {
            Assert.Equal("sign", u.Action);
            Assert.Equal(AuthorizationResourceSpecKind.Context, u.Resource.Kind);
            Assert.Equal("urn:altinn:task:Task_1", u.Resource.AdditionalResourceAttribute);
            Assert.Equal(AuthorizationAccessRestriction.None, u.AccessRestriction);
        });
        Assert.Equal(new[] { DialogParty, OtherParty }.Order(StringComparer.Ordinal), guiCheck.Units.Select(u => u.Party));

        var contextUnits = checks.SelectMany(c => c.Units).ToList();

        // Dialog attachment: explicit action override is honored
        Assert.Contains(contextUnits, c => c is
        {
            Action: "attachmentread",
            Resource.AdditionalResourceAttribute: "urn:altinn:subresource:secret"
        });

        // Transmission: no explicit action => defaults to read
        Assert.Contains(contextUnits, c => c is
        {
            Action: Constants.ReadAction,
            Resource: { Kind: AuthorizationResourceSpecKind.Context, ServiceResource: "urn:altinn:resource:other-service" }
        });

        // Transmission attachment: no explicit action => defaults to read
        Assert.Contains(contextUnits, c => c is
        {
            Action: Constants.ReadAction,
            Resource.AdditionalResourceAttribute: "urn:altinn:task:Task_2"
        });

        // Navigational action: no explicit action => defaults to read
        Assert.Contains(contextUnits, c => c is
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
        Assert.Equal(Constants.ReadAction, Assert.Single(check.Units).Action);
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
        Assert.Equal("customread", Assert.Single(check.Units).Action);
    }

    [Fact]
    public void ContextWithEmptyPartiesAndNoDialogPartyChecksShouldProduceCheckWithNoUnits()
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
                DialogPartyChecks = []
            }
        };

        var check = transmission.GetAuthorizationCheck(new DialogEntity { Party = DialogParty });

        Assert.NotNull(check);
        Assert.Empty(check.Units);
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

    private const string ViaParty = "urn:altinn:organization:identifier-no:310778737";

    [Fact]
    public void DialogPartyChecksShouldExpandIntoUnitsAtTheDialogPartyWithTheirAccessRestriction()
    {
        var transmission = new DialogTransmission
        {
            AuthorizationContext = new DialogTransmissionAuthorizationContext
            {
                Parties = [OtherParty],
                DialogPartyChecks =
                [
                    new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy },
                    new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.ClientDelegation, ViaParty = ViaParty },
                    new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.None, ServiceResource = "urn:altinn:resource:r-full" }
                ]
            }
        };

        var check = transmission.GetAuthorizationCheck(new DialogEntity { Party = DialogParty });

        Assert.NotNull(check);
        Assert.Equal(4, check.Units.Count);
        Assert.Contains(check.Units, u => u is
        {
            Party: OtherParty, AccessRestriction: AuthorizationAccessRestriction.None, ViaParty: null,
            Resource.ServiceResource: null
        });
        Assert.Contains(check.Units, u => u is
        {
            Party: DialogParty, AccessRestriction: AuthorizationAccessRestriction.DirectAndHierarchy, ViaParty: null,
            Resource.ServiceResource: null
        });
        Assert.Contains(check.Units, u => u is
        {
            Party: DialogParty, AccessRestriction: AuthorizationAccessRestriction.ClientDelegation, ViaParty: ViaParty,
            Resource.ServiceResource: null
        });
        Assert.Contains(check.Units, u => u is
        {
            Party: DialogParty, AccessRestriction: AuthorizationAccessRestriction.None, ViaParty: null,
            Resource.ServiceResource: "urn:altinn:resource:r-full"
        });
        Assert.All(check.Units, u => Assert.Equal(Constants.ReadAction, u.Action));
    }

    [Theory]
    // Action falls back to the context's, then "read"; resource falls back to the context's (null = the dialog's)
    [InlineData(null, null, null, null, Constants.ReadAction, null)]
    [InlineData("readall", null, null, null, "readall", null)]
    [InlineData("readall", null, null, "urn:altinn:resource:context-resource", "readall", "urn:altinn:resource:context-resource")]
    [InlineData(null, "urn:altinn:resource:r-full", "sign", null, "sign", "urn:altinn:resource:r-full")]
    [InlineData("readall", "urn:altinn:resource:r-full", "sign", "urn:altinn:resource:context-resource", "readall", "urn:altinn:resource:r-full")]
    public void DialogPartyCheckShouldFallBackToTheContextsRight(
        string? checkAction, string? checkResource, string? contextAction, string? contextResource,
        string expectedAction, string? expectedResource)
    {
        var transmission = new DialogTransmission
        {
            AuthorizationContext = new DialogTransmissionAuthorizationContext
            {
                Action = contextAction,
                ServiceResource = contextResource,
                AdditionalResourceAttribute = "urn:altinn:task:Task_1",
                DialogPartyChecks =
                [
                    new()
                    {
                        AccessRestriction = DialogPartyCheckAccessRestriction.Values.None,
                        Action = checkAction,
                        ServiceResource = checkResource
                    }
                ]
            }
        };

        var check = transmission.GetAuthorizationCheck(new DialogEntity { Party = DialogParty });

        Assert.NotNull(check);
        var unit = Assert.Single(check.Units);
        Assert.Equal(expectedAction, unit.Action);
        Assert.Equal(AuthorizationResourceSpecKind.Context, unit.Resource.Kind);
        Assert.Equal(expectedResource, unit.Resource.ServiceResource);
        // The context's additional resource attribute is layered on every unit's effective resource
        Assert.Equal("urn:altinn:task:Task_1", unit.Resource.AdditionalResourceAttribute);
        Assert.Equal(DialogParty, unit.Party);
    }

    [Fact]
    public void ChecksDifferingOnlyInAccessRestrictionShouldNotBeEqual()
    {
        var dialog = new DialogEntity { Party = DialogParty };
        var checks = new[]
        {
            TransmissionWithCheck(DialogPartyCheckAccessRestriction.Values.None, null).GetAuthorizationCheck(dialog),
            TransmissionWithCheck(DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy, null).GetAuthorizationCheck(dialog),
            TransmissionWithCheck(DialogPartyCheckAccessRestriction.Values.ClientDelegation, ViaParty).GetAuthorizationCheck(dialog),
            TransmissionWithCheck(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OtherParty).GetAuthorizationCheck(dialog)
        };

        Assert.Equal(checks.Length, checks.Distinct().Count());
        Assert.Equal(checks.Length, checks.Select(x => x!.CanonicalIdentity).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void UnrestrictedDialogPartyCheckShouldEqualTheEquivalentPartiesCheck()
    {
        // A [None] check is the replacement for the former includeDialogParty: true
        var dialog = new DialogEntity { Party = DialogParty };
        var viaCheck = new DialogTransmission
        {
            AuthorizationContext = new DialogTransmissionAuthorizationContext
            {
                DialogPartyChecks = [new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.None }]
            }
        }.GetAuthorizationCheck(dialog);
        var viaParties = new DialogTransmission
        {
            AuthorizationContext = new DialogTransmissionAuthorizationContext { Parties = [DialogParty] }
        }.GetAuthorizationCheck(dialog);

        Assert.Equal(viaParties, viaCheck);
    }

    private static DialogTransmission TransmissionWithCheck(DialogPartyCheckAccessRestriction.Values restriction, string? viaParty) => new()
    {
        AuthorizationContext = new DialogTransmissionAuthorizationContext
        {
            DialogPartyChecks = [new() { AccessRestriction = restriction, ViaParty = viaParty }]
        }
    };
}
