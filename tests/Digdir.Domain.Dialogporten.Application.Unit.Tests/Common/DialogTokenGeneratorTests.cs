using System.Security.Claims;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Common;

public class DialogTokenGeneratorTests
{
    private const string IssuerVersion = "/api/v1";
    private const string DialogParty = "urn:altinn:organization:identifier-no:991825827";
    private const string OtherParty = "urn:altinn:organization:identifier-no:912345678";

    private Dictionary<string, object?>? _capturedClaims;
    private string? _capturedTokenType;

    [Fact]
    public void DialogTokenShouldOmitContextGrantsAndUseDialogTokenType()
    {
        // Arrange
        var generator = CreateGenerator();
        var dialog = CreateDialog();

        var contextCheck = new AuthorizationCheck(
            "sign",
            AuthorizationResourceSpec.FromContext(null, "urn:altinn:task:Task_1"),
            [DialogParty, OtherParty]);

        var authorizationResult = new DialogDetailsAuthorizationResult
        {
            AuthorizedChecks =
            [
                AuthorizedCheck.FullyPermitted(new AuthorizationCheck(
                    "read", AuthorizationResourceSpec.Main, [DialogParty])),
                AuthorizedCheck.FullyPermitted(new AuthorizationCheck(
                    "write",
                    AuthorizationResourceSpec.FromLegacyAuthorizationAttribute("urn:altinn:task:Task_1"),
                    [DialogParty])),
                // Fully permitted, including for the dialog party — must still not appear in "a"
                AuthorizedCheck.FullyPermitted(contextCheck)
            ]
        };

        // Act
        generator.GetDialogToken(dialog, authorizationResult, IssuerVersion);

        // Assert
        Assert.Equal(DialogTokenTypes.DialogToken, _capturedTokenType);
        Assert.NotNull(_capturedClaims);
        Assert.Equal("read;write,urn:altinn:task:Task_1", _capturedClaims[DialogTokenClaimTypes.Actions]);
        Assert.False(_capturedClaims.ContainsKey(DialogTokenClaimTypes.EntityId));
        Assert.False(_capturedClaims.ContainsKey(DialogTokenClaimTypes.PermittedParties));
    }

    [Fact]
    public void ContextTokenShouldCarryEntityGrantAndPermittedParties()
    {
        // Arrange
        var generator = CreateGenerator();
        var dialog = CreateDialog();
        var entityId = Guid.NewGuid();

        var check = new AuthorizationCheck(
            "sign",
            AuthorizationResourceSpec.FromContext(null, "urn:altinn:task:Task_1"),
            [DialogParty, OtherParty]);

        // Only a subset of the parties was permitted by the PDP
        var authorizedCheck = new AuthorizedCheck(check, [OtherParty]);

        // Act
        generator.GetDialogContextToken(dialog, authorizedCheck, entityId,
            DialogContextTokenEntityTypes.GuiAction, IssuerVersion);

        // Assert
        Assert.Equal(DialogTokenTypes.DialogContextToken, _capturedTokenType);
        Assert.NotNull(_capturedClaims);
        Assert.Equal(entityId, _capturedClaims[DialogTokenClaimTypes.EntityId]);
        Assert.Equal(DialogContextTokenEntityTypes.GuiAction, _capturedClaims[DialogTokenClaimTypes.EntityType]);
        Assert.Equal("sign", _capturedClaims[DialogTokenClaimTypes.Actions]);
        Assert.False(_capturedClaims.ContainsKey(DialogTokenClaimTypes.EffectiveResource));
        Assert.Equal("urn:altinn:task:Task_1", _capturedClaims[DialogTokenClaimTypes.AdditionalResourceAttribute]);
        Assert.Equal(dialog.Party, _capturedClaims[DialogTokenClaimTypes.DialogParty]);
        Assert.Equal(dialog.ServiceResource, _capturedClaims[DialogTokenClaimTypes.ServiceResource]);
        Assert.Equal(dialog.Id, _capturedClaims[DialogTokenClaimTypes.DialogId]);

        var permittedParties = Assert.IsAssignableFrom<IReadOnlyList<string>>(
            _capturedClaims[DialogTokenClaimTypes.PermittedParties]);
        Assert.Equal([OtherParty], permittedParties);
    }

    [Fact]
    public void ContextTokenShouldCarryServiceResourceAsEffectiveResource()
    {
        // Arrange
        var generator = CreateGenerator();
        var check = new AuthorizationCheck(
            "read",
            AuthorizationResourceSpec.FromContext("urn:altinn:resource:other-service", null),
            [OtherParty]);

        // Act
        generator.GetDialogContextToken(CreateDialog(), AuthorizedCheck.FullyPermitted(check),
            Guid.NewGuid(), DialogContextTokenEntityTypes.Transmission, IssuerVersion);

        // Assert
        Assert.NotNull(_capturedClaims);
        Assert.Equal("urn:altinn:resource:other-service", _capturedClaims[DialogTokenClaimTypes.EffectiveResource]);
        Assert.False(_capturedClaims.ContainsKey(DialogTokenClaimTypes.AdditionalResourceAttribute));
    }

    [Fact]
    public void ContextTokenShouldCarryBothResourceOverrideAndAdditionalAttributeWhenBothPresent()
    {
        // A recipient must be able to reconstruct the exact same PDP request from the token alone, so
        // neither field may be dropped when both are set on the same check.
        // Arrange
        var generator = CreateGenerator();
        var check = new AuthorizationCheck(
            "sign",
            AuthorizationResourceSpec.FromContext("urn:altinn:resource:other-service", "urn:altinn:task:Task_1"),
            [OtherParty]);

        // Act
        generator.GetDialogContextToken(CreateDialog(), AuthorizedCheck.FullyPermitted(check),
            Guid.NewGuid(), DialogContextTokenEntityTypes.Transmission, IssuerVersion);

        // Assert
        Assert.NotNull(_capturedClaims);
        Assert.Equal("urn:altinn:resource:other-service", _capturedClaims[DialogTokenClaimTypes.EffectiveResource]);
        Assert.Equal("urn:altinn:task:Task_1", _capturedClaims[DialogTokenClaimTypes.AdditionalResourceAttribute]);
    }

    [Fact]
    public void ContextTokenShouldOmitResourceClaimsWhenCheckTargetsDialogResource()
    {
        // Arrange
        var generator = CreateGenerator();
        var check = new AuthorizationCheck(
            "read",
            AuthorizationResourceSpec.FromContext(null, null),
            [OtherParty]);

        // Act
        generator.GetDialogContextToken(CreateDialog(), AuthorizedCheck.FullyPermitted(check),
            Guid.NewGuid(), DialogContextTokenEntityTypes.Attachment, IssuerVersion);

        // Assert
        Assert.NotNull(_capturedClaims);
        Assert.False(_capturedClaims.ContainsKey(DialogTokenClaimTypes.EffectiveResource));
        Assert.False(_capturedClaims.ContainsKey(DialogTokenClaimTypes.AdditionalResourceAttribute));
    }

    private static DialogEntity CreateDialog() => new()
    {
        Id = Guid.NewGuid(),
        Party = DialogParty,
        ServiceResource = "urn:altinn:resource:some-service"
    };

    private DialogTokenGenerator CreateGenerator()
    {
        var settings = new ApplicationSettings
        {
            Dialogporten = new DialogportenSettings
            {
                BaseUri = new Uri("https://unittest"),
                Ed25519KeyPairs = new Ed25519KeyPairs
                {
                    Primary = new Ed25519KeyPair { Kid = "kid1", PrivateComponent = "", PublicComponent = "" },
                    Secondary = new Ed25519KeyPair { Kid = "kid2", PrivateComponent = "", PublicComponent = "" }
                }
            }
        };

        var user = Substitute.For<IUser>();
        user.GetPrincipal().Returns(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("pid", "22834498646"),
            new Claim("acr", "idporten-loa-high")
        ])));

        var clock = Substitute.For<IClock>();
        clock.UtcNowOffset.Returns(DateTimeOffset.UnixEpoch);

        var jwsGenerator = Substitute.For<ICompactJwsGenerator>();
        jwsGenerator.GetCompactJws(
                Arg.Do<Dictionary<string, object?>>(claims => _capturedClaims = claims),
                Arg.Do<string>(tokenType => _capturedTokenType = tokenType))
            .Returns("jws");

        return new DialogTokenGenerator(new OptionsMock<ApplicationSettings>(settings), user, clock, jwsGenerator);
    }
}
