using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DeleteDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Deleting_Dialog_Should_Set_DeletedAt()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createDialogResponse = await Application.Send(createDialogCommand);

        // Act
        var dialogId = createDialogResponse.AsT0.DialogId;
        var deleteDialogCommand = new DeleteDialogCommand { Id = dialogId };
        await Application.Send(deleteDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = dialogId };
        var getDialogResponse = await Application.Send(getDialogQuery);

        // Assert
        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();
        dialog.Should().NotBeNull();
        dialog.DeletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Updating_Deleted_Dialog_Should_Return_EntityDeleted()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createDialogResponse = await Application.Send(createDialogCommand);

        var dialogId = createDialogResponse.AsT0.DialogId;
        var deleteDialogCommand = new DeleteDialogCommand { Id = dialogId };
        await Application.Send(deleteDialogCommand);

        var getDialogQuery = new GetDialogQuery { DialogId = dialogId };
        var getDialogResponse = await Application.Send(getDialogQuery);
        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(dialog);

        // Act
        var updateDialogCommand = new UpdateDialogCommand { Id = dialogId, Dto = updateDialogDto };
        var updateDialogResponse = await Application.Send(updateDialogCommand);

        // Assert
        updateDialogResponse.TryPickT2(out var entityDeleted, out _).Should().BeTrue();
        entityDeleted.Should().NotBeNull();
        entityDeleted.Message.Should().Contain(dialogId.ToString());
    }

    [Fact]
    public async Task DeleteDialogCommand_Should_Return_New_Revision()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createDialogResponse = await Application.Send(createDialogCommand);

        var dialogId = createDialogResponse.AsT0.DialogId;
        var oldRevision = createDialogResponse.AsT0.Revision;

        // Act
        var deleteDialogCommand = new DeleteDialogCommand { Id = dialogId };
        var deleteDialogResponse = await Application.Send(deleteDialogCommand);

        // Assert
        deleteDialogResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        success.Revision.Should().NotBeEmpty();
        success.Revision.Should().NotBe(oldRevision);
    }

}
