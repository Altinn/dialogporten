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
            $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}713330310");
        var dialogId = request.DialogId;

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Request);
        Assert.NotNull(result.Request.Resource);
        Assert.NotNull(result.Request.Action);
        Assert.NotNull(result.Request.AccessSubject);
        Assert.NotNull(result.Request.MultiRequests);

        // Check AccessSubject attributes
        var accessSubject = result.Request.AccessSubject.First();
        Assert.Equal("s1", accessSubject.Id);
        Assert.Contains(accessSubject.Attribute, a => a.AttributeId == "urn:altinn:person:identifier-no" && a.Value == "12345678901");
        Assert.Single(accessSubject.Attribute);

        // Check Action attributes.
        var actionIdsByName = new Dictionary<string, string>();
        Assert.Equal(request.AltinnActions.Select(x => x.Name).Distinct().Count(), result.Request.Action.Count);
        foreach (var action in request.AltinnActions.Select(x => x.Name))
        {
            var actionElement = result.Request.Action.FirstOrDefault(a => a.Attribute.Any(attr => attr.AttributeId == "urn:oasis:names:tc:xacml:1.0:action:action-id" && attr.Value == action));
            Assert.NotNull(actionElement);
            actionIdsByName[action] = actionElement.Id;
        }

        // Check Resource attributes
        var resourceIdsBySubresource = new Dictionary<string, string>();
        Assert.Equal(request.AltinnActions.Select(x => x.AuthorizationAttribute).Distinct().Count(), result.Request.Resource.Count);
        foreach (var subresource in request.AltinnActions.Select(x => x.AuthorizationAttribute))
        {
            var resource = result.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:subresource" && a.Value == subresource));
            Assert.NotNull(resource);
            Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-service");
            Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:organization:identifier-no" && a.Value == "713330310");
            Assert.Contains(resource.Attribute, a => a.AttributeId == "urn:altinn:resourceinstance" && a.Value == dialogId.ToString());
            resourceIdsBySubresource[subresource] = resource.Id;
        }

        // Check MultiRequests
        Assert.Equal(request.AltinnActions.Count, result.Request.MultiRequests.RequestReference.Count);
        foreach (var altinnAction in request.AltinnActions)
        {
            Assert.Contains(result.Request.MultiRequests.RequestReference, rr
                => ContainsSameElements(rr.ReferenceId, new List<string> { "s1", resourceIdsBySubresource[altinnAction.AuthorizationAttribute], actionIdsByName[altinnAction.Name] }));
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
            $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}713330310");

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Check AccessSubject attributes
        var accessSubject = result.Request.AccessSubject.First();
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
            $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}713330310",
            isApp: true);

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Request);
        Assert.NotNull(result.Request.Resource);

        // Check Resource attributes
        var resource1 = result.Request.Resource.FirstOrDefault(r => r.Id == "r1");
        Assert.NotNull(resource1);
        Assert.Contains(resource1.Attribute, a => a.AttributeId == "urn:altinn:org" && a.Value == "ttd");
        Assert.Contains(resource1.Attribute, a => a.AttributeId == "urn:altinn:app" && a.Value == "some-app_with_underscores");

        // We cannot support instance id for apps since we don't have a partyId
        // Assert.Contains(resource1.Attribute, a => a.AttributeId == "urn:altinn:instance-id" && a.Value == dialogId.ToString());
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForSystemUser()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("authorization_details", AuthorizationDetailsClaimValue)
            ),
            $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}713330310"
            );

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Request);
        Assert.NotNull(result.Request.Resource);

        var accessSubject = result.Request.AccessSubject.First();
        Assert.Equal("s1", accessSubject.Id);
        Assert.Contains(accessSubject.Attribute, a => a.AttributeId == "urn:altinn:systemuser:uuid" && a.Value == "unique_systemuser_id");
        Assert.Single(accessSubject.Attribute);
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForAltinn2SiUser()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("urn:altinn:partyid", "1"),
                ("urn:altinn:userid", "2"),
                ("urn:altinn:username", "someusername")
            ),
            $"{AltinnSelfIdentifiedUserIdentifier.PrefixWithSeparator}someusername"
        );

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Request);
        Assert.NotNull(result.Request.Resource);

        var accessResource = result.Request.Resource.First();
        Assert.Contains(accessResource.Attribute, a => a.AttributeId == "urn:altinn:partyid" && a.Value == "1");

        var accessSubject = result.Request.AccessSubject.First();
        Assert.Equal("s1", accessSubject.Id);
        Assert.Contains(accessSubject.Attribute, a => a.AttributeId == "urn:altinn:userid" && a.Value == "2");
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
                ("email", "foo@bar.com")
            ),
            $"{IdportenSelfIdentifiedUserIdentifier.PrefixWithSeparator}foo@bar.com"
        );

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Request);
        Assert.NotNull(result.Request.Resource);

        var accessResource = result.Request.Resource.First();
        Assert.Contains(accessResource.Attribute, a => a.AttributeId == "urn:altinn:partyid" && a.Value == "1");

        var accessSubject = result.Request.AccessSubject.First();
        Assert.Equal("s1", accessSubject.Id);
        Assert.Contains(accessSubject.Attribute, a => a.AttributeId == "urn:altinn:userid" && a.Value == "2");
        Assert.Single(accessSubject.Attribute);
    }

    [Fact]
    public void CreateDialogDetailsRequestShouldReturnCorrectRequestForFeideUser()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("urn:altinn:partyid", "1"),
                ("urn:altinn:userid", "2"),
                ("orgsub", "33a633c47ef2f656978f957532ce6d0de6f5e13f1e0618b37b4b2a70573e5551")
            ),
            $"{FeideUserIdentifier.PrefixWithSeparator}33a633c47ef2f656978f957532ce6d0de6f5e13f1e0618b37b4b2a70573e5551"
        );

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Request);
        Assert.NotNull(result.Request.Resource);

        var accessResource = result.Request.Resource.First();
        Assert.Contains(accessResource.Attribute, a => a.AttributeId == "urn:altinn:partyid" && a.Value == "1");

        var accessSubject = result.Request.AccessSubject.First();
        Assert.Equal("s1", accessSubject.Id);
        Assert.Contains(accessSubject.Attribute, a => a.AttributeId == "urn:altinn:userid" && a.Value == "2");
        Assert.Single(accessSubject.Attribute);
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

        // Add an action that has a resource override
        request.AltinnActions.Add(new AltinnAction("read", "urn:altinn:resource:some-other-service"));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        // Find the resource having an attribute set with value "some-other-service"
        var resource = result.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-other-service"));
        Assert.NotNull(resource);
        // Check that there are no other resources with the same attribute and no resource instance attribute set
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-service");
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resourceinstance");
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

        // Add an action that has a resource override
        request.AltinnActions.Add(new AltinnAction("read", "urn:altinn:resource:app_ttd_some-other-service"));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        // Find the resource having an attribute set with value "some-other-service"
        var resource = result.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:app" && a.Value == "some-other-service"));
        Assert.NotNull(resource);
        // Check that there are no other resources with the same attribute and no resource instance attribute set
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resource" && a.Value == "some-service");
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:resourceinstance");
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

        // Add an action that has a resource override
        request.AltinnActions.Add(new AltinnAction("read", "urn:altinn:task:Task_1"));

        // Act
        var result = DecisionRequestHelper.CreateDialogDetailsRequest(request);

        // Assert
        // Find the resource having an attribute set with value "some-other-service"
        var resource = result.Request.Resource.FirstOrDefault(r => r.Attribute.Any(a => a.AttributeId == "urn:altinn:task" && a.Value == "Task_1"));
        Assert.NotNull(resource);
        // Check that there are implicit subresource attribute set
        Assert.DoesNotContain(resource.Attribute, a => a.AttributeId == "urn:altinn:subresource");
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

        // Add an action to the request that the mocked response should give a non-permit response failaction
        request.AltinnActions.Add(new AltinnAction("failaction", Constants.MainResource));

        var jsonRequestRoot = DecisionRequestHelper.CreateDialogDetailsRequest(request);
        var jsonResponse = CreateMockedXamlJsonResponse(jsonRequestRoot);

        // Act
        var response = DecisionRequestHelper.CreateDialogDetailsResponse(request.AltinnActions, jsonResponse);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(request.AltinnActions.Count - 2, response.AuthorizedAltinnActions.Count);
        Assert.Contains(new AltinnAction("read", Constants.MainResource), response.AuthorizedAltinnActions);
        Assert.Contains(new AltinnAction("write", Constants.MainResource), response.AuthorizedAltinnActions);
        Assert.Contains(new AltinnAction("sign", "element1"), response.AuthorizedAltinnActions);
        Assert.Contains(new AltinnAction("elementread", "element2"), response.AuthorizedAltinnActions);
        Assert.DoesNotContain(new AltinnAction("elementread", "element3"), response.AuthorizedAltinnActions);
        Assert.DoesNotContain(new AltinnAction("failaction", Constants.MainResource), response.AuthorizedAltinnActions);
    }

    [Fact]
    public void CreateDetailsRequestShouldThrowUnreachableExceptionIfNoValidUserType()
    {
        // Arrange
        var request = CreateDialogDetailsAuthorizationRequest(
            GetAsClaims(
                ("consumer", "somevalue")
            ),
            $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}713330310");

        // Act / assert
        Assert.Throws<UnreachableException>(() => DecisionRequestHelper.CreateDialogDetailsRequest(request));
    }


    private static DialogDetailsAuthorizationRequest CreateDialogDetailsAuthorizationRequest(List<Claim> principalClaims, string party, bool isApp = false)
    {
        var allClaims = new List<Claim>
        {
            new("urn:altinn:foo", "bar")
        };
        allClaims.AddRange(principalClaims);
        return new DialogDetailsAuthorizationRequest
        {
            ClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(allClaims)),
            ServiceResource = isApp ? "urn:altinn:app:app_ttd_some-app_with_underscores" : "urn:altinn:resource:some-service",
            DialogId = Guid.NewGuid(),

            // This should be copied resources with attributes "urn:altinn:organizationnumber" if starting with "urn:altinn:organization:identifier-no::"
            // and "urn:altinn:ssn" if starting with "urn:altinn:person:identifier-no::"
            Party = party,
            AltinnActions =
            [
                new("read", Constants.MainResource),
                new("sign", "element1"),
                new("write", Constants.MainResource),
                new("elementread", "element3"),
                new("elementread", "element2")
            ]
        };
    }

    private static XacmlJsonResponse CreateMockedXamlJsonResponse(XacmlJsonRequestRoot request)
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
            var resourceName = request.Request.Resource.First(r => r.Id == resourceId).Attribute.FirstOrDefault(x => x.AttributeId == "urn:altinn:subresource")?.Value;

            var shouldFail = actionName == "failaction" || resourceName == "element3";

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
