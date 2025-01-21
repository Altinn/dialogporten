using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DeletedDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Fetching_Deleted_Dialog_Should_Return_Gone()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createDialogResponse = await Application.Send(createDialogCommand);

        var dialogId = createDialogResponse.AsT0.DialogId;
        var deleteDialogCommand = new DeleteDialogCommand { Id = dialogId };
        await Application.Send(deleteDialogCommand);

        // Act
        var getDialogQuery = new GetDialogQuery { DialogId = dialogId };
        var getDialogResponse = await Application.Send(getDialogQuery);

        // Assert
        getDialogResponse.TryPickT2(out var entityDeleted, out _).Should().BeTrue();
        entityDeleted.Should().NotBeNull();
        entityDeleted.Message.Should().Contain("is removed");
        entityDeleted.Message.Should().Contain(dialogId.ToString());
    }
}
