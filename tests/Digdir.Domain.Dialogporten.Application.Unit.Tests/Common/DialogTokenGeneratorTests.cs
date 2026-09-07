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
    public void DialogTokenShouldKeepActionsAtLegacySemanticsAndUseDialogTokenType()
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
        generator.GetDialogToken(dialog, authorizationResult, [], IssuerVersion);

        // Assert
        Assert.Equal(DialogTokenTypes.DialogToken, _capturedTokenType);
        Assert.NotNull(_capturedClaims);
        Assert.Equal("read;write,urn:altinn:task:Task_1", _capturedClaims[DialogTokenClaimTypes.Actions]);
    }

    [Fact]
    public void DialogTokenShouldOmitAuthorizedEntitiesClaimWhenThereAreNone()
    {
        // Arrange
        var generator = CreateGenerator();

        // Act
        generator.GetDialogToken(CreateDialog(), new DialogDetailsAuthorizationResult(), [], IssuerVersion);

        // Assert
        Assert.NotNull(_capturedClaims);
        Assert.False(_capturedClaims.ContainsKey(DialogTokenClaimTypes.AuthorizedEntities));
    }

    [Fact]
    public void DialogTokenShouldCarryDistinctAuthorizedEntityReferencesInOrder()
    {
        // Arrange
        var generator = CreateGenerator();
        var transmissionId = Guid.NewGuid().ToString();

        // Act: two contexts sharing a service owner supplied reference collapse into one entry
        generator.GetDialogToken(
            CreateDialog(),
            new DialogDetailsAuthorizationResult(),
            [transmissionId, "my-own-reference", "my-own-reference"],
            IssuerVersion);

        // Assert
        Assert.NotNull(_capturedClaims);
        var authorizedEntities = Assert.IsType<string[]>(_capturedClaims[DialogTokenClaimTypes.AuthorizedEntities]);
        Assert.Equal([transmissionId, "my-own-reference"], authorizedEntities);
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
