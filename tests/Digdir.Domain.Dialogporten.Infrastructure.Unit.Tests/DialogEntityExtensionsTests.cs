using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.Authorization;
using Xunit;

namespace Digdir.Domain.Dialogporten.Infrastructure.Unit.Tests;

public class DialogEntityExtensionsTests
{
    [Fact]
    public void GetAltinnActionsShouldReturnCorrectActionsForTransmissionAuthorizationAttributes()
    {
        // Arrange
        var dialogEntity = new DialogEntity
        {
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
        var actions = dialogEntity.GetAltinnActions();

        // Assert
        Assert.NotNull(actions);
        Assert.True(actions.Count == 9);
        Assert.Contains(actions, a => a is { Name: Constants.ReadAction, AuthorizationAttribute: Constants.MainResource });
        Assert.Contains(actions, a => a is { Name: Constants.ReadAction, AuthorizationAttribute: "foo" });
        Assert.Contains(actions, a => a is { Name: Constants.TransmissionReadAction, AuthorizationAttribute: "bar" });
        Assert.Contains(actions, a => a is { Name: "apiread", AuthorizationAttribute: Constants.MainResource });
        Assert.Contains(actions, a => a is { Name: "guiread", AuthorizationAttribute: Constants.MainResource });
        Assert.Contains(actions, a => a is { Name: Constants.TransmissionReadAction, AuthorizationAttribute: "urn:altinn:subresource:bar" });
        Assert.Contains(actions, a => a is { Name: Constants.TransmissionReadAction, AuthorizationAttribute: "urn:altinn:task:Task_1" });
        Assert.Contains(actions, a => a is { Name: Constants.ReadAction, AuthorizationAttribute: "urn:altinn:resource:some-service:element1" });
        Assert.Contains(actions, a => a is { Name: Constants.ReadAction, AuthorizationAttribute: "urn:altinn:resource:app_ttd_some-app" });
    }
}
