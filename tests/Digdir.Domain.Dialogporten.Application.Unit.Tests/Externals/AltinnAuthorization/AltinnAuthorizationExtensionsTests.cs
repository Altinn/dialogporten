using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using NSubstitute;

namespace Digdir.Domain.Dialogporten.Application.Unit.Tests.Externals.AltinnAuthorization;

public class AltinnAuthorizationExtensionsTests
{
    private readonly IAltinnAuthorization _altinnAuthorization = Substitute.For<IAltinnAuthorization>();
    private readonly DialogEntity _dialog = new();

    [Fact]
    public async Task GetDialogAccess_Grants_Access_When_Access_To_Main_Resource_Without_Checking_List_Authorization()
    {
        // Arrange
        var authorizationResult = new DialogDetailsAuthorizationResult
        {
            AuthorizedChecks =
            [
                AuthorizedCheck.FullyPermitted(new AuthorizationCheck(
                    Constants.ReadAction, AuthorizationResourceSpec.Main, []))
            ]
        };
        _altinnAuthorization
            .GetDialogDetailsAuthorization(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
            .Returns(authorizationResult);

        // Act
        var (hasAccess, authorization) = await _altinnAuthorization.GetDialogAccess(_dialog, TestContext.Current.CancellationToken);

        // Assert
        hasAccess.Should().BeTrue();
        authorization.Should().BeSameAs(authorizationResult);
        await _altinnAuthorization.DidNotReceiveWithAnyArgs()
            .HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDialogAccess_Falls_Back_To_List_Authorization_When_No_Access_To_Main_Resource()
    {
        // Arrange
        var authorizationResult = new DialogDetailsAuthorizationResult { AuthorizedChecks = [] };
        _altinnAuthorization
            .GetDialogDetailsAuthorization(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
            .Returns(authorizationResult);
        _altinnAuthorization
            .HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var (hasAccess, authorization) = await _altinnAuthorization.GetDialogAccess(_dialog, TestContext.Current.CancellationToken);

        // Assert
        hasAccess.Should().BeTrue();
        authorization.Should().BeSameAs(authorizationResult);
    }

    [Fact]
    public async Task GetDialogAccess_Denies_Access_When_No_Access_To_Main_Resource_And_No_List_Authorization()
    {
        // Arrange
        var authorizationResult = new DialogDetailsAuthorizationResult { AuthorizedChecks = [] };
        _altinnAuthorization
            .GetDialogDetailsAuthorization(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
            .Returns(authorizationResult);
        _altinnAuthorization
            .HasListAuthorizationForDialog(Arg.Any<DialogEntity>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var (hasAccess, authorization) = await _altinnAuthorization.GetDialogAccess(_dialog, TestContext.Current.CancellationToken);

        // Assert
        hasAccess.Should().BeFalse();
        authorization.Should().BeSameAs(authorizationResult);
    }
}
