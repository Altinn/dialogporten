using Digdir.Domain.Dialogporten.Application.Features.V1.Common.AuthorizationContexts;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.AuthorizationContexts;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Features.V1.Common.AuthorizationContexts;

public class AuthorizationContextDtoValidatorTests
{
    private readonly AuthorizationContextDtoValidator _validator = new();

    [Theory]
    [InlineData("urn:altinn:task:Task_1")] // does not expand into an app identity (no 'app_' tail)
    [InlineData("urn:altinn:subresource:mycustomresource")]
    [InlineData(null)]
    public void Should_Allow_Non_App_AdditionalResourceAttribute(string? additionalResourceAttribute)
    {
        var result = _validator.Validate(CreateDto(additionalResourceAttribute));

        Assert.True(result.IsValid, string.Join(", ", result.Errors));
    }

    [Theory]
    [InlineData("urn:altinn:app:app_org_myapp")] // explicit app namespace - ServiceResource is the field for apps
    [InlineData("urn:altinn:task:app_other_sensitive-app")] // app_ tail smuggled under a different namespace
    [InlineData("urn:altinn:integration:app_other_sensitive-app")]
    [InlineData("urn:altinn:app:not-an-app-id")] // app namespace stated even though the value isn't app_-shaped
    public void Should_Reject_AdditionalResourceAttribute_That_References_An_App(string additionalResourceAttribute)
    {
        // AdditionalResourceAttribute has no app use case: ServiceResource already carries the resource-registry
        // entry for an app, and app/org expansion must never happen for this field, however it's spelled.
        var result = _validator.Validate(CreateDto(additionalResourceAttribute));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("urn:altinn:org:brg")]
    [InlineData("URN:ALTINN:ORG:brg")]
    public void Should_Reject_AdditionalResourceAttribute_That_References_An_Organization(string additionalResourceAttribute)
    {
        // The org namespace is the other half of an app identity: an app-backed service resource already
        // renders as an app/org pair, so a caller-supplied org lands as a second org value in the same
        // resource category and can satisfy another organization's policy target.
        var result = _validator.Validate(CreateDto(additionalResourceAttribute));

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("my-own-reference")]
    public void Should_Allow_Absent_Or_Non_Empty_TokenRef(string? tokenRef)
    {
        var result = _validator.Validate(CreateDto(null, tokenRef));

        Assert.True(result.IsValid, string.Join(", ", result.Errors));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Reject_Empty_Or_Whitespace_TokenRef(string tokenRef)
    {
        // A token reference is emitted verbatim in the dialog token's "e" claim; a blank one is unusable there.
        var result = _validator.Validate(CreateDto(null, tokenRef));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AuthorizationContextDto.TokenRef));
    }

    [Fact]
    public void Should_Reject_TokenRef_Longer_Than_Max_Token_Reference_Length()
    {
        var result = _validator.Validate(CreateDto(null, new string('x', AuthorizationContext.MaxTokenReferenceLength + 1)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AuthorizationContextDto.TokenRef));
    }

    private const string OrgParty = "urn:altinn:organization:identifier-no:310778737";
    private const string OtherOrgParty = "urn:altinn:organization:identifier-no:991825827";
    private const string PersonParty = "urn:altinn:person:identifier-no:08895699684";

    private static DialogPartyCheckDto Check(
        DialogPartyCheckAccessRestriction.Values accessRestriction,
        string? viaParty = null,
        string? serviceResource = null,
        string? action = null) => new()
        {
            AccessRestriction = accessRestriction,
            ViaParty = viaParty,
            ServiceResource = serviceResource,
            Action = action
        };

    private static AuthorizationContextDto CreateDtoWithChecks(params DialogPartyCheckDto[] checks)
    {
        var dto = CreateDto(null);
        dto.DialogPartyChecks = [.. checks];
        return dto;
    }

    public static TheoryData<DialogPartyCheckDto[]> ValidDialogPartyChecks => new()
    {
        new[] { Check(DialogPartyCheckAccessRestriction.Values.None) },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy) },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OrgParty) },
        new[]
        {
            Check(DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy),
            Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OrgParty),
            Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OtherOrgParty),
            Check(DialogPartyCheckAccessRestriction.Values.None, serviceResource: "urn:altinn:resource:r-full")
        },
        // A None check with another action does not cover a ClientDelegation check on the context's right
        new[]
        {
            Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OrgParty),
            Check(DialogPartyCheckAccessRestriction.Values.None, action: "readall")
        },
        // Explicitly naming a resource is a different right than inheriting the context's/dialog's
        new[]
        {
            Check(DialogPartyCheckAccessRestriction.Values.None),
            Check(DialogPartyCheckAccessRestriction.Values.None, serviceResource: "urn:altinn:resource:r-full")
        }
    };

    [Theory]
    [MemberData(nameof(ValidDialogPartyChecks))]
    public void Should_Allow_Valid_DialogPartyChecks(DialogPartyCheckDto[] checks)
    {
        var result = _validator.Validate(CreateDtoWithChecks(checks));

        Assert.True(result.IsValid, string.Join(", ", result.Errors));
    }

    public static TheoryData<DialogPartyCheckDto[]> InvalidDialogPartyChecks => new()
    {
        // Undefined access restriction (including the missing/default value)
        new[] { Check(0) },
        new[] { Check((DialogPartyCheckAccessRestriction.Values)42) },
        // ClientDelegation without via party
        new[] { Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation) },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, "") },
        // Via party with another access restriction
        new[] { Check(DialogPartyCheckAccessRestriction.Values.None, OrgParty) },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy, OrgParty) },
        // Via party that is not an organization
        new[] { Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, PersonParty) },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, "310778737") },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, "urn:altinn:organization:identifier-no:123456789") },
        // Invalid service resource / action
        new[] { Check(DialogPartyCheckAccessRestriction.Values.None, serviceResource: "urn:altinn:task:Task_1") },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.None, serviceResource: "not a resource") },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.None, action: "") },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.None, action: new string('x', 256)) },
        // Duplicates
        new[] { Check(DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy), Check(DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy) },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OrgParty), Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OrgParty) },
        // "read" is the effective action when none is given, so these are duplicates too
        new[] { Check(DialogPartyCheckAccessRestriction.Values.None), Check(DialogPartyCheckAccessRestriction.Values.None, action: "read") },
        // Restricted checks covered by a None check for the same effective right
        new[] { Check(DialogPartyCheckAccessRestriction.Values.None), Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OrgParty) },
        new[] { Check(DialogPartyCheckAccessRestriction.Values.None), Check(DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy) },
        new[]
        {
            Check(DialogPartyCheckAccessRestriction.Values.None, serviceResource: "urn:altinn:resource:r-full", action: "readall"),
            Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OrgParty, "urn:altinn:resource:r-full", "readall")
        }
    };

    [Theory]
    [MemberData(nameof(InvalidDialogPartyChecks))]
    public void Should_Reject_Invalid_DialogPartyChecks(DialogPartyCheckDto[] checks)
    {
        var result = _validator.Validate(CreateDtoWithChecks(checks));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Reject_Null_DialogPartyCheck_Without_Throwing()
    {
        var dto = CreateDtoWithChecks(Check(DialogPartyCheckAccessRestriction.Values.None));
        dto.DialogPartyChecks.Add(null!);

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_Treat_A_Check_Inheriting_The_Contexts_Right_As_A_Duplicate_Of_One_Naming_It()
    {
        var dto = CreateDtoWithChecks(
            Check(DialogPartyCheckAccessRestriction.Values.None),
            Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OrgParty, "urn:altinn:resource:ctx", "sign"));
        dto.ServiceResource = "urn:altinn:resource:ctx";
        dto.Action = "sign";

        var result = _validator.Validate(dto);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.PropertyName == nameof(AuthorizationContextDto.DialogPartyChecks));
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, true)]
    [InlineData(0, 1, true)]
    [InlineData(4, 0, true)]
    [InlineData(1, 3, true)]
    [InlineData(4, 1, false)]
    [InlineData(2, 3, false)]
    public void Should_Require_Between_One_And_Four_Entries_Across_Parties_And_DialogPartyChecks(
        int numberOfParties, int numberOfChecks, bool expectedValid)
    {
        string[] parties = [OrgParty, OtherOrgParty, PersonParty, "urn:altinn:organization:identifier-no:713330310"];
        DialogPartyCheckDto[] checks =
        [
            Check(DialogPartyCheckAccessRestriction.Values.DirectAndHierarchy),
            Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OrgParty),
            Check(DialogPartyCheckAccessRestriction.Values.ClientDelegation, OtherOrgParty),
            Check(DialogPartyCheckAccessRestriction.Values.None, action: "readall")
        ];
        var dto = CreateDtoWithChecks([.. checks.Take(numberOfChecks)]);
        dto.Parties = [.. parties.Take(numberOfParties)];

        var result = _validator.Validate(dto);

        Assert.Equal(expectedValid, result.IsValid);
        if (!expectedValid)
        {
            Assert.Contains(result.Errors, x => x.PropertyName == nameof(AuthorizationContextDto.Parties));
        }
    }

    private static AuthorizationContextDto CreateDto(string? additionalResourceAttribute, string? tokenRef = null) => new()
    {
        AdditionalResourceAttribute = additionalResourceAttribute,
        DialogPartyChecks = [new() { AccessRestriction = DialogPartyCheckAccessRestriction.Values.None }],
        TokenRef = tokenRef,
        UnauthorizedPresentation = AuthorizationContextUnauthorizedPresentation.Values.Disabled
    };
}
