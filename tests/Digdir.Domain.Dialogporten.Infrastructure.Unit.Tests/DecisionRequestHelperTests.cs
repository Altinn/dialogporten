using System.Diagnostics;
using System.Security.Claims;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Parties;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class DecisionRequestHelperTests
{
    private const string AuthorizationDetailsClaimValue = /*lang=json,strict*/"[{\"type\":\"urn:altinn:systemuser\",\"systemuser_id\":[\"unique_systemuser_id\"]}]";
    private static readonly string OrgParty = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}713330310";
    private static readonly string OtherOrgParty = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}991825827";
    private static readonly string ViaOrgParty = $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}310778737";
    private const string AccessRestrictionAttributeId = "urn:altinn:authorization:access-restriction";
    private const string ViaPartyAttributeId = "urn:altinn:via-party:organization:identifier-no";

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequest()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                // This should not be copied as subject claim since there's a "pid"-claim
                ("authorization_details", AuthorizationDetailsClaimValue),
                ("pid", "12345678901")
            ),
            OrgParty);
        var instanceRef = request.InstanceRef;

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Request.Request);
        Assert.NotNull(result.Request.Request.Resource);
        Assert.NotNull(result.Request.Request.Action);
        Assert.NotNull(result.Request.Request.AccessSubject);
        Assert.NotNull(result.Request.Request.MultiRequests);

        // Check AccessSubject attributes
        var accessSubject = result.Request.Request.AccessSubject.First();
        Assert.Equal("s1", accessSubject.Id);
        Assert.Contains(accessSubject.Attribute, a => a.AttributeId == "urn:altinn:person:identifier-no" && a.Value == "12345678901");
        Assert.Single(accessSubject.Attribute);

        // Check Action attributes.
        var actionIdsByName = new Dictionary<string, string>();
        // Every check in the fixture is a single, unrestricted unit
        var units = request.Checks.Select(x => x.Units.Single()).ToList();
        Assert.Equal(units.Select(x => x.Action).Distinct().Count(), result.Request.Request.Action.Count);
        foreach (var action in units.Select(x => x.Action))
        {
            var actionElement = result.Request.Request.Action.FirstOrDefault(a => a.Attribute.Any(attr => attr.AttributeId == "urn:oasis:names:tc:xacml:1.0:action:action-id" && attr.Value == action));
            Assert.NotNull(actionElement);
            actionIdsByName[action] = actionElement.Id;
        }

        // Check Resource attributes: the main resource is rendered with the legacy "main" sentinel as a
        // subresource attribute, so every check in the fixture resolves via urn:altinn:subresource.
        var resourceIdsBySubresource = new Dictionary<string, string>();
        Assert.Equal(units.Select(x => x.Resource).Distinct().Count(), result.Request.Request.Resource.Count);
        foreach (var subresource in request.Checks.Select(GetExpectedSubresource))
        {
            var resource = result.Request.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:subresource" && a.Value == subresource));
            Assert.NotNull(resource);
            Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-service");
            Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:organization:identifier-no" && a.Value == "713330310");
            Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:resource:instance-id" && a.Value == instanceRef.Value);
            // Unrestricted units keep the wire format unchanged
            Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == AccessRestrictionAttributeId);
            Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == ViaPartyAttributeId);
            resourceIdsBySubresource[subresource] = resource.Id;
        }

        // Check MultiRequests and tuple correlation
        Assert.Equal(request.Checks.Count, result.Request.Request.MultiRequests.RequestReference.Count);
        Assert.Equal(request.Checks.Count, result.Tuples.Count);
        Assert.Equal(result.Request.Request.MultiRequests.RequestReference.Count, result.ExpectedResults);
        foreach (var check in request.Checks)
        {
            Assert.Contains(result.Request.Request.MultiRequests.RequestReference, rr
                => ContainsSameElements(rr.ReferenceId, new List<string> { "s1", resourceIdsBySubresource[GetExpectedSubresource(check)], actionIdsByName[check.Units.Single().Action] }));
        }
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForExchangedTokens()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                // This should not be copied as subject claim since there's a "urn:altinn:user-id"-claim
                ("pid", "12345678901"),
                ("urn:altinn:userid", "5678901")
            ),
            OrgParty);

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Check AccessSubject attributes
        var accessSubject = result.Request.Request.AccessSubject.First();
        Assert.Equal("s1", accessSubject.Id);
        Assert.Contains(accessSubject.Attribute, a => a.AttributeId == "urn:altinn:userid" && a.Value == "5678901");
        Assert.Single(accessSubject.Attribute);
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForApp()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("pid", "12345678901")
            ),
            OrgParty,
            isApp: true);

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        // Check Resource attributes
        var resource1 = result.Request.Request.Resource.FirstOrDefault(r => r.Id == "r1");
        Assert.NotNull(resource1);
        Assert.Contains(resource1.Attribute, a => a.AttributeId == "urn:altinn:org" && a.Value == "ttd");
        Assert.Contains(resource1.Attribute, a => a.AttributeId == "urn:altinn:app" && a.Value == "some-app_with_underscores");
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForSystemUser()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("authorization_details", AuthorizationDetailsClaimValue)
            ),
            OrgParty);

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        var accessSubject = result.Request.Request.AccessSubject.First();
        Assert.Equal("s1", accessSubject.Id);
        Assert.Contains(accessSubject.Attribute, a => a.AttributeId == "urn:altinn:systemuser:uuid" && a.Value == "unique_systemuser_id");
        Assert.Single(accessSubject.Attribute);
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForIdPortenEmailUser()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("urn:altinn:partyid", "1"),
                ("urn:altinn:userid", "2"),
                ("email", "foo@bar.com"),
                ("amr", "Selfregistered-email")
            ),
            $"{IdportenEmailUserIdentifier.PrefixWithSeparator}foo@bar.com"
        );

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        var accessResource = result.Request.Request.Resource.First();
        Assert.Contains(accessResource.Attribute, a => a.AttributeId == "urn:altinn:partyid" && a.Value == "1");

        var accessSubject = result.Request.Request.AccessSubject.First();
        Assert.Equal("s1", accessSubject.Id);
        Assert.Contains(accessSubject.Attribute, a => a.AttributeId == "urn:altinn:userid" && a.Value == "2");
        Assert.Single(accessSubject.Attribute);
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldOnlyRewriteTheSelfIdentifiedUsersOwnParty()
    {
        // Arrange: a context check referring a foreign party in addition to the user's own party
        var ownParty = $"{IdportenEmailUserIdentifier.PrefixWithSeparator}foo@bar.com";
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("urn:altinn:partyid", "1"),
                ("urn:altinn:userid", "2"),
                ("email", "foo@bar.com"),
                ("amr", "Selfregistered-email")
            ),
            ownParty
        );
        request.Checks.Add(new AuthorizationCheck(
            "read",
            AuthorizationResourceSpec.FromContext("urn:altinn:resource:some-other-service", null),
            [ownParty, OtherOrgParty]));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert: the user's own party is rewritten to partyid in every category it appears in,
        // while the foreign party is passed through verbatim.
        var overriddenResources = result.Request.Request.Resource
            .Where(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-other-service"))
            .ToList();
        Assert.Equal(2, overriddenResources.Count);
        Assert.Contains(overriddenResources, r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:partyid" && a.Value == "1"));
        Assert.Contains(overriddenResources, r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:organization:identifier-no" && a.Value == "991825827"));
        Assert.DoesNotContain(result.Request.Request.Resource.SelectMany(r => r.Attribute),
            a => a.AttributeId == IdportenEmailUserIdentifier.Prefix);
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForOverriddenResource()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("pid", "12345678901")
            ),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}16073422888");

        // Add a check that has a legacy resource override
        request.Checks.Add(LegacyCheck("read", "urn:altinn:resource:some-other-service", request.Party));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        // Find the resource having an attribute set with value "some-other-service"
        var resource = result.Request.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-other-service"));
        Assert.NotNull(resource);
        // Check that there are no other resources with the same attribute and no resource instance attribute set
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-service");
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resource:instance-id");
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForOverriddenResourceForApp()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("pid", "12345678901")
            ),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}16073422888");

        // Add a check that has a legacy resource override
        request.Checks.Add(LegacyCheck("read", "urn:altinn:resource:app_ttd_some-other-service", request.Party));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        // Find the resource having an attribute set with value "some-other-service"
        var resource = result.Request.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:app" && a.Value == "some-other-service"));
        Assert.NotNull(resource);
        // Check that there are no other resources with the same attribute and no resource instance attribute set
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-service");
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resource:instance-id");
        // Check that we have an org attribute
        Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:org" && a.Value == "ttd");
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForFullyQualifiedSubresource()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("pid", "12345678901")
            ),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}16073422888");

        request.Checks.Add(LegacyCheck("read", "urn:altinn:task:Task_1", request.Party));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        var resource = result.Request.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:task" && a.Value == "Task_1"));
        Assert.NotNull(resource);
        // Check that there are no implicit subresource attribute set
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:subresource");
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldOverrideResourceAndDropInstanceIdForContextServiceResource()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(("pid", "12345678901")),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}16073422888");

        request.Checks.Add(new AuthorizationCheck(
            "read",
            AuthorizationResourceSpec.FromContext("urn:altinn:resource:some-other-service", "urn:altinn:task:Task_1"),
            [OtherOrgParty]));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert: the context resource replaces the dialog's resource and instance reference, keeps the
        // party, and layers the additional resource attribute on top.
        var resource = result.Request.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-other-service"));
        Assert.NotNull(resource);
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-service");
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resource:instance-id");
        Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:organization:identifier-no" && a.Value == "991825827");
        Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:task" && a.Value == "Task_1");
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldKeepInstanceIdForContextWithOnlyAdditionalResourceAttribute()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(("pid", "12345678901")),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}16073422888");
        var instanceRef = request.InstanceRef;

        request.Checks.Add(new AuthorizationCheck(
            "read",
            AuthorizationResourceSpec.FromContext(null, "urn:altinn:task:Task_2"),
            [OtherOrgParty]));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert: without a resource override the dialog's own resource and instance reference still apply.
        var resource = result.Request.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:task" && a.Value == "Task_2"));
        Assert.NotNull(resource);
        Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-service");
        Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:resource:instance-id" && a.Value == instanceRef.Value);
        Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:organization:identifier-no" && a.Value == "991825827");
    }

    [Theory]
    [InlineData("urn:altinn:task:app_other_sensitive-app", "urn:altinn:task", "app_other_sensitive-app")]
    [InlineData("urn:altinn:app:app_other_sensitive-app", "urn:altinn:app", "app_other_sensitive-app")]
    [InlineData("urn:altinn:app:foo_bar", "urn:altinn:app", "foo_bar")]
    public void CreateDialogDetailsRequestShouldNeverExpandAdditionalResourceAttributeIntoAppOrOrg(
        string additionalResourceAttribute, string expectedAttributeId, string expectedValue)
    {
        // Unlike the service resource and legacy authorization attribute, additionalResourceAttribute is
        // rendered as a single literal attribute - split on the last colon only, never reinterpreted as an
        // app/org attribute pair, whatever its namespace or value look like.
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(("pid", "12345678901")),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}16073422888");

        request.Checks.Add(new AuthorizationCheck(
            "read",
            AuthorizationResourceSpec.FromContext(null, additionalResourceAttribute),
            [OtherOrgParty]));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert: the literal (namespace, value) split is present verbatim, and - the definitive sign that no
        // app/org expansion occurred - no separate org attribute was derived from the value.
        var resource = result.Request.Request.Resource.FirstOrDefault(
            r => r.Attribute.Any(a => a.AttributeId == expectedAttributeId && a.Value == expectedValue));
        Assert.NotNull(resource);
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:org");
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldFanOutOneResourceCategoryPerParty()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(("pid", "12345678901")),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}16073422888");

        var multiPartyCheck = new AuthorizationCheck(
            "read",
            AuthorizationResourceSpec.FromContext("urn:altinn:resource:some-other-service", null),
            [OrgParty, OtherOrgParty]);
        request.Checks.Add(multiPartyCheck);

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert: one resource category (and one request reference) per (resource, party) combination
        var overriddenResources = result.Request.Request.Resource
            .Where(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-other-service"))
            .ToList();
        Assert.Equal(2, overriddenResources.Count);
        Assert.Contains(overriddenResources, r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:organization:identifier-no" && a.Value == "713330310"));
        Assert.Contains(overriddenResources, r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:organization:identifier-no" && a.Value == "991825827"));

        // Both tuples point back to the same check
        Assert.Equal(2, result.Tuples.Count(t => t.Check == multiPartyCheck));
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldRenderAccessRestrictionAttributesPerUnit()
    {
        // Arrange: the same right and party under all three access restrictions
        var request = CreateDialogDetailsAuthorizationRequest(GetAsClaims(("pid", "12345678901")), OrgParty);
        var resource = AuthorizationResourceSpec.FromContext(null, null);
        var check = new AuthorizationCheck(
        [
            new AuthorizationUnit("read", resource, OrgParty),
            new AuthorizationUnit("read", resource, OrgParty, AuthorizationAccessRestriction.DirectAndHierarchy),
            new AuthorizationUnit("read", resource, OrgParty, AuthorizationAccessRestriction.ClientDelegation, ViaOrgParty)
        ]);
        request.Checks.Clear();
        request.Checks.Add(check);

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert: one multirequest, one resource category and one reference per unit
        Assert.Equal(3, result.Request.Request.Resource.Count);
        Assert.Equal(3, result.Request.Request.MultiRequests.RequestReference.Count);
        Assert.Equal(3, result.ExpectedResults);
        Assert.Equal(3, result.Tuples.Select(x => x.ReferenceIndex).Distinct().Count());

        var categories = result.Request.Request.Resource;
        Assert.All(categories, c =>
        {
            Assert.Contains(c.Attribute, a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-service");
            Assert.Contains(c.Attribute, a => a.AttributeId == "urn:altinn:organization:identifier-no" && a.Value == "713330310");
        });

        Assert.Single(categories, c => c.Attribute.All(a => a.AttributeId is not (AccessRestrictionAttributeId or ViaPartyAttributeId)));

        var directAndHierarchy = Assert.Single(categories, c => c.Attribute.Any(a => a is { AttributeId: AccessRestrictionAttributeId, Value: "DirectAndHierarchy" }));
        Assert.DoesNotContain(directAndHierarchy.Attribute, a => a.AttributeId == ViaPartyAttributeId);

        var clientDelegation = Assert.Single(categories, c => c.Attribute.Any(a => a is { AttributeId: AccessRestrictionAttributeId, Value: "ClientDelegation" }));
        // The bare organization number, never the URN
        Assert.Single(clientDelegation.Attribute, a => a is { AttributeId: ViaPartyAttributeId, Value: "310778737" });
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldNotShareResourceCategoriesAcrossViaParties()
    {
        var request = CreateDialogDetailsAuthorizationRequest(GetAsClaims(("pid", "12345678901")), OrgParty);
        var resource = AuthorizationResourceSpec.FromContext(null, null);
        request.Checks.Add(new AuthorizationCheck([new AuthorizationUnit("read", resource, OrgParty, AuthorizationAccessRestriction.ClientDelegation, ViaOrgParty)]));
        request.Checks.Add(new AuthorizationCheck([new AuthorizationUnit("read", resource, OrgParty, AuthorizationAccessRestriction.ClientDelegation, OtherOrgParty)]));
        // Identical to the first: collapses into the same evaluation
        request.Checks.Add(new AuthorizationCheck(
        [
            new AuthorizationUnit("read", resource, OrgParty, AuthorizationAccessRestriction.ClientDelegation, ViaOrgParty),
            new AuthorizationUnit("read", resource, OtherOrgParty)
        ]));

        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        var viaPartyValues = result.Request.Request.Resource
            .SelectMany(c => c.Attribute)
            .Where(a => a.AttributeId == ViaPartyAttributeId)
            .Select(a => a.Value)
            .ToList();
        Assert.Equal(2, viaPartyValues.Count);
        Assert.Contains("310778737", viaPartyValues);
        Assert.Contains("991825827", viaPartyValues);

        var viaOrgTuples = result.Tuples.Where(t => t.Unit.ViaParty == ViaOrgParty).ToList();
        Assert.Equal(2, viaOrgTuples.Count);
        Assert.Single(viaOrgTuples.Select(t => t.ReferenceIndex).Distinct());
    }

    [Fact]
    public void CreateDialogDetailsResponseShouldOnlyPermitTheUnitsThePdpPermitted()
    {
        // Arrange: the PDP denies the restricted units and permits the unrestricted one
        var request = CreateDialogDetailsAuthorizationRequest(GetAsClaims(("pid", "12345678901")), OrgParty);
        var resource = AuthorizationResourceSpec.FromContext(null, null);
        var unrestricted = new AuthorizationUnit("read", resource, OrgParty);
        var restrictedOnly = new AuthorizationCheck(
        [
            new AuthorizationUnit("read", resource, OrgParty, AuthorizationAccessRestriction.DirectAndHierarchy),
            new AuthorizationUnit("read", resource, OrgParty, AuthorizationAccessRestriction.ClientDelegation, ViaOrgParty)
        ]);
        var mixed = new AuthorizationCheck([unrestricted, .. restrictedOnly.Units]);
        request.Checks.Add(restrictedOnly);
        request.Checks.Add(mixed);

        var preparedRequest = DecisionRequestHelper.CreateDialogDetailsRequest(request);
        var jsonResponse = CreateMockedXamlJsonResponse(preparedRequest.Request, denyResourceCategoryPredicate:
            r => r.Attribute.Any(a => a.AttributeId == AccessRestrictionAttributeId));

        // Act
        var response = DecisionRequestHelper.CreateDialogDetailsResponse(preparedRequest, jsonResponse);

        // Assert
        Assert.False(response.HasAccess(restrictedOnly));
        Assert.True(response.HasAccess(mixed));
        var authorizedMixed = Assert.Single(response.AuthorizedChecks, x => x.Check == mixed);
        Assert.Equal([unrestricted], authorizedMixed.PermittedUnits);
    }

    [Fact]
    public void RestrictedMainResourceShapedUnitsMustNotSatisfyMainResourcePredicates()
    {
        // Defense in depth: only unrestricted units can answer the dialog-level predicates.
        var restricted = new AuthorizationCheck(
            [new AuthorizationUnit("read", AuthorizationResourceSpec.Main, OrgParty, AuthorizationAccessRestriction.DirectAndHierarchy)]);
        var result = new DialogDetailsAuthorizationResult { AuthorizedChecks = [AuthorizedCheck.FullyPermitted(restricted)] };

        Assert.True(result.HasAccess(restricted));
        Assert.False(result.HasAccessToMainResource());
        Assert.False(result.HasReadAccessToMainResource());
    }

    [Fact]
    public void CreateDialogDetailsResponseShouldReturnCorrectResponse()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("pid", "12345678901")
            ),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}12345678901");

        // Add a check that the mocked response should give a non-permit response for
        request.Checks.Add(MainCheck("failaction", request.Party));

        var preparedRequest = DecisionRequestHelper.CreateDialogDetailsRequest(request);
        var jsonResponse = CreateMockedXamlJsonResponse(preparedRequest.Request);

        // Act
        var response = DecisionRequestHelper.CreateDialogDetailsResponse(preparedRequest, jsonResponse);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(request.Checks.Count - 2, response.AuthorizedChecks.Count);
        Assert.True(response.HasAccess(MainCheck("read", request.Party)));
        Assert.True(response.HasAccess(MainCheck("write", request.Party)));
        Assert.True(response.HasAccess(LegacyCheck("sign", "element1", request.Party)));
        Assert.True(response.HasAccess(LegacyCheck("elementread", "element2", request.Party)));
        Assert.False(response.HasAccess(LegacyCheck("elementread", "element3", request.Party)));
        Assert.False(response.HasAccess(MainCheck("failaction", request.Party)));

        // The authorized checks carry the units the PDP permitted
        var authorizedCheck = response.AuthorizedChecks.First(x => x.Check == MainCheck("read", request.Party));
        Assert.Equal([request.Party], authorizedCheck.PermittedUnits.Select(x => x.Party));
    }

    [Fact]
    public void CreateDialogDetailsResponseShouldAuthorizeCheckWhenAnyPartyIsPermitted()
    {
        // Arrange: a multi-party check where the mock PDP denies one of the parties
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(("pid", "12345678901")),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}12345678901");

        var multiPartyCheck = new AuthorizationCheck(
            "read",
            AuthorizationResourceSpec.FromContext("urn:altinn:resource:some-other-service", null),
            [OrgParty, OtherOrgParty]);
        request.Checks.Add(multiPartyCheck);

        var preparedRequest = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Deny the category carrying OrgParty; permit everything else
        var jsonResponse = CreateMockedXamlJsonResponse(preparedRequest.Request, denyResourceCategoryPredicate:
            r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:organization:identifier-no" && a.Value == "713330310"));

        // Act
        var response = DecisionRequestHelper.CreateDialogDetailsResponse(preparedRequest, jsonResponse);

        // Assert: the check is authorized (OR over parties), and only the permitted party is echoed
        Assert.True(response.HasAccess(multiPartyCheck));
        var authorizedCheck = response.AuthorizedChecks.First(x => x.Check == multiPartyCheck);
        Assert.Equal([OtherOrgParty], authorizedCheck.PermittedUnits.Select(x => x.Party));
    }

    [Fact]
    public void CreateDialogDetailsResponseShouldDenyEverythingOnResultCountMismatch()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(("pid", "12345678901")),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}12345678901");

        var preparedRequest = DecisionRequestHelper.CreateDialogDetailsRequest(request);
        var jsonResponse = CreateMockedXamlJsonResponse(preparedRequest.Request);
        jsonResponse.Response.RemoveAt(jsonResponse.Response.Count - 1);

        // Act
        var response = DecisionRequestHelper.CreateDialogDetailsResponse(preparedRequest, jsonResponse);

        // Assert: decisions cannot be correlated reliably, so everything is denied
        Assert.Empty(response.AuthorizedChecks);
    }

    [Fact]
    public void CreateDialogDetailsResponseShouldDenyEverythingOnNullResponse()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(("pid", "12345678901")),
            $"{NorwegianPersonIdentifier.PrefixWithSeparator}12345678901");

        var preparedRequest = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Act
        var response = DecisionRequestHelper.CreateDialogDetailsResponse(preparedRequest, null);

        // Assert
        Assert.Empty(response.AuthorizedChecks);
    }

    [Fact]
    public void CreateDetailsRequestShouldThrowUnreachableExceptionIfNoValidUserType()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("consumer", "somevalue")
            ),
            OrgParty);

        // Act / assert
        Assert.Throws<UnreachableException>(() => DecisionRequestHelper.CreateDialogDetailsRequest(request));
    }

    [Fact]
    public void GenerateCacheKeyShouldChangeWhenAnyPdpRelevantCheckInputChanges()
    {
        // Arrange
        var claims = GetAsClaims(("pid", "12345678901"));
        var baseline = CreateDialogDetailsAuthorizationRequest(claims, OrgParty);
        var baselineKey = baseline.GenerateCacheKey();

        var variations = new List<AuthorizationCheck>
        {
            // Different action
            new("write", AuthorizationResourceSpec.FromContext("urn:altinn:resource:x", null), [OrgParty]),
            // Different service resource
            new("read", AuthorizationResourceSpec.FromContext("urn:altinn:resource:y", null), [OrgParty]),
            // Different additional resource attribute
            new("read", AuthorizationResourceSpec.FromContext("urn:altinn:resource:x", "urn:altinn:task:Task_1"), [OrgParty]),
            // Different party set
            new("read", AuthorizationResourceSpec.FromContext("urn:altinn:resource:x", null), [OrgParty, OtherOrgParty]),
            // Legacy attribute expressing the same resource must not collide with the context form
            LegacyCheck("read", "urn:altinn:resource:x", OrgParty),
            // Different access restriction / via party for the same right and party
            new([new AuthorizationUnit("read", AuthorizationResourceSpec.FromContext("urn:altinn:resource:x", null), OrgParty,
                AuthorizationAccessRestriction.DirectAndHierarchy)]),
            new([new AuthorizationUnit("read", AuthorizationResourceSpec.FromContext("urn:altinn:resource:x", null), OrgParty,
                AuthorizationAccessRestriction.ClientDelegation, OtherOrgParty)]),
            new([new AuthorizationUnit("read", AuthorizationResourceSpec.FromContext("urn:altinn:resource:x", null), OrgParty,
                AuthorizationAccessRestriction.ClientDelegation, ViaOrgParty)])
        };

        var keys = new List<string> { baselineKey };
        var contextBaseline = CreateDialogDetailsAuthorizationRequest(claims, OrgParty);
        contextBaseline.Checks.Add(new AuthorizationCheck("read", AuthorizationResourceSpec.FromContext("urn:altinn:resource:x", null), [OrgParty]));
        keys.Add(contextBaseline.GenerateCacheKey());

        foreach (var variation in variations)
        {
            var request = CreateDialogDetailsAuthorizationRequest(claims, OrgParty);
            request.Checks.Add(variation);
            keys.Add(request.GenerateCacheKey());
        }

        // Assert: every variation produces a distinct key
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void GenerateCacheKeyShouldUseTheVersionedNamespace()
    {
        // The cached DialogDetailsAuthorizationResult changed shape (PermittedParties -> PermittedUnits). The
        // version in the key keeps entries written by the previous shape from being deserialized as the new one;
        // bump it whenever the cached result type changes.
        var request = CreateDialogDetailsAuthorizationRequest(GetAsClaims(("pid", "12345678901")), OrgParty);

        Assert.Matches("^auth:details:v2:[0-9a-f]{64}$", request.GenerateCacheKey());
    }

    private static AuthorizationCheck MainCheck(string action, string party) =>
        new(action, AuthorizationResourceSpec.Main, [party]);

    private static AuthorizationCheck LegacyCheck(string action, string authorizationAttribute, string party) =>
        new(action, AuthorizationResourceSpec.FromLegacyAuthorizationAttribute(authorizationAttribute), [party]);

    private static string GetExpectedSubresource(AuthorizationCheck check) =>
        check.Units.Single().Resource is { Kind: AuthorizationResourceSpecKind.Main }
            ? Constants.MainResource
            : check.Units.Single().Resource.LegacyAuthorizationAttribute!;

    private static DialogDetailsAuthorizationRequest CreateDialogDetailsAuthorizationRequest(List<Claim> principalClaims, string party, bool isApp = false)
    {
        var allClaims = new List<Claim>
        {
            new("urn:altinn:foo", "bar")
        };
        allClaims.AddRange(principalClaims);
        var dialogId = Guid.NewGuid();
        return new DialogDetailsAuthorizationRequest
        {
            ClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(allClaims)),
            ServiceResource = isApp ? "urn:altinn:app:app_ttd_some-app_with_underscores" : "urn:altinn:resource:some-service",
            InstanceRef = new InstanceRef(InstanceRefType.DialogId, dialogId, InstanceRef.CreateDialogRef(dialogId)),
            Party = party,
            Checks =
            [
                MainCheck("read", party),
                LegacyCheck("sign", "element1", party),
                MainCheck("write", party),
                LegacyCheck("elementread", "element3", party),
                LegacyCheck("elementread", "element2", party)
            ]
        };
    }

    private static XacmlJsonResponse CreateMockedXamlJsonResponse(
        XacmlJsonRequestRoot request,
        Func<XacmlJsonCategory, bool>? denyResourceCategoryPredicate = null)
    {
        var response = new XacmlJsonResponse
        {
            Response = []
        };

        foreach (var requestReference in request.Request.MultiRequests.RequestReference)
        {
            // Check if this request reference refers to the action with name "failaction", in which case we should return a non-permit response
            // We need to use the actionId since the action name is not included in the request reference
            var actionId = requestReference.ReferenceId.First(x => x.StartsWith('a'));
            var resourceId = requestReference.ReferenceId.First(x => x.StartsWith('r'));
            var actionName = request.Request.Action.First(a => a.Id == actionId).Attribute.First().Value;
            var resourceCategory = request.Request.Resource.First(r => r.Id == resourceId);
            var resourceName = resourceCategory.Attribute.FirstOrDefault(x => x.AttributeId == "urn:altinn:subresource")?.Value;

            var shouldFail = actionName == "failaction"
                || resourceName == "element3"
                || (denyResourceCategoryPredicate?.Invoke(resourceCategory) ?? false);

            response.Response.Add(new XacmlJsonResult
            {
                Decision = shouldFail ? "Deny" : "Permit"
            });
        }

        return response;
    }

    private static List<Claim> GetAsClaims(params (string, string)[] claims)
        => claims.Select(c => new Claim(c.Item1, c.Item2)).ToList();

    private static bool ContainsSameElements(IEnumerable<string> collection, IEnumerable<string> expectedElements) =>
        expectedElements.All(collection.Contains) && collection.Count() == expectedElements.Count();
}
