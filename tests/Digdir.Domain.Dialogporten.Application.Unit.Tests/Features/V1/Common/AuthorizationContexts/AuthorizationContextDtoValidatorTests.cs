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

    private static AuthorizationContextDto CreateDto(string? additionalResourceAttribute) => new()
    {
        AdditionalResourceAttribute = additionalResourceAttribute,
        IncludeDialogParty = true,
        UnauthorizedPresentation = AuthorizationContextUnauthorizedPresentation.Values.Disabled
    };
}
