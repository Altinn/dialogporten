using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.ServiceOwnerContexts.Entities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using ServiceOwnerLabelDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.ServiceOwnerLabelDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateServiceOwnerLabelsTests : ApplicationCollectionFixture
{
    public UpdateServiceOwnerLabelsTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Can_Update_Dialog_With_ServiceOwner_Labels()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        createDialogCommand.Dto.ServiceOwnerLabels = [new() { Value = "Sel" }];

        await Application.Send(createDialogCommand);

        var getDialogCommand = new GetDialogQuery
        {
            DialogId = dialogId,
        };

        var getDialogResponse = await Application.Send(getDialogCommand);
        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(dialog);
        updateDialogDto.ServiceOwnerLabels.Add(new ServiceOwnerLabelDto { Value = "Scadrial" });


        // Act
        var response = await Application.Send(new UpdateDialogCommand
        {
            Dto = updateDialogDto,
            Id = dialogId,
        });

        // Assert
        response.TryPickT0(out _, out _).Should().BeTrue();
        var serviceOwnerLabels = await Application.GetDbEntities<ServiceOwnerLabel>();
        serviceOwnerLabels.Should().HaveCount(2);
    }

    [Fact]
    public async Task Can_Remove_Existing_Labels_On_Dialog_Update()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        createDialogCommand.Dto.ServiceOwnerLabels = [new() { Value = "Sel" }];

        await Application.Send(createDialogCommand);

        var getDialogCommand = new GetDialogQuery
        {
            DialogId = dialogId,
        };

        var getDialogResponse = await Application.Send(getDialogCommand);
        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(dialog);

        const string newValue = "Scadrial";
        updateDialogDto.ServiceOwnerLabels = [new() { Value = newValue }];

        // Act
        var response = await Application.Send(new UpdateDialogCommand
        {
            Dto = updateDialogDto,
            Id = dialogId,
        });

        // Assert
        response.TryPickT0(out _, out _).Should().BeTrue();
        var serviceOwnerLabels = await Application.GetDbEntities<ServiceOwnerLabel>();

        serviceOwnerLabels.Should()
            .HaveCount(1)
            .And
            .ContainSingle(x => x.Value
                .Equals(newValue, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Cannot_Update_Dialog_With_Duplicate_ServiceOwner_Labels()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        createDialogCommand.Dto.ServiceOwnerLabels = [new() { Value = "Sel" }];

        await Application.Send(createDialogCommand);

        var getDialogCommand = new GetDialogQuery
        {
            DialogId = dialogId,
        };

        var getDialogResponse = await Application.Send(getDialogCommand);
        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(dialog);
        updateDialogDto.ServiceOwnerLabels.Add(new() { Value = "Sel" });

        // Act
        var response = await Application.Send(new UpdateDialogCommand
        {
            Dto = updateDialogDto,
            Id = dialogId,
        });

        // Assert
        response.TryPickT3(out var validationError, out _).Should().BeTrue();
        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("duplicate"));
    }

    [Fact]
    public async Task Cannot_Add_ServiceOwner_Labels_With_Invalid_Length_On_Dialog_Update()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        await Application.Send(createDialogCommand);

        var getDialogCommand = new GetDialogQuery
        {
            DialogId = dialogId,
        };

        var getDialogResponse = await Application.Send(getDialogCommand);
        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(dialog);

        updateDialogDto.ServiceOwnerLabels.Add(new() { Value = "a" });
        updateDialogDto.ServiceOwnerLabels.Add(new() { Value = new string('a', 300) });

        // Act
        var response = await Application.Send(new UpdateDialogCommand
        {
            Dto = updateDialogDto,
            Id = dialogId,
        });

        // Assert
        response.TryPickT3(out var validationError, out _).Should().BeTrue();
        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("at least"));

        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("or fewer"));
    }

    [Fact]
    public async Task Updating_Only_ServiceOwnerLabels_Should_Update_ServiceOwnerContext_Revision_But_Not_Dialog_Revision()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        await Application.Send(createDialogCommand);
        var getDialogCommand = new GetDialogQuery
        {
            DialogId = dialogId,
        };

        var getDialogResponse = await Application.Send(getDialogCommand);
        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();

        var originalServiceOwnerContextRevision =
            (await Application.GetDbEntities<Domain.ServiceOwnerContexts.Entities.ServiceOwnerContext>())
            .Single(x => x.DialogId == dialogId).Revision;

        var originalDialogRevision = dialog.Revision;

        var mapper = Application.GetMapper();
        var updateDialogDto = mapper.Map<UpdateDialogDto>(dialog);
        updateDialogDto.ServiceOwnerLabels.Add(new() { Value = "Scadrial" });

        // Act
        await Application.Send(new UpdateDialogCommand
        {
            Dto = updateDialogDto,
            Id = dialogId,
        });

        // Assert
        var getDialogResponseAfterUpdate = await Application.Send(getDialogCommand);
        getDialogResponseAfterUpdate.TryPickT0(out var updatedDialog, out _).Should().BeTrue();
        updatedDialog.Revision.Should().Be(originalDialogRevision);

        var serviceOwnerLabels = await Application.GetDbEntities<ServiceOwnerLabel>();
        serviceOwnerLabels.Should().HaveCount(1);

        var updatedServiceOwnerContextRevision =
            (await Application.GetDbEntities<Domain.ServiceOwnerContexts.Entities.ServiceOwnerContext>())
            .Single(x => x.DialogId == dialogId).Revision;
        updatedServiceOwnerContextRevision.Should().NotBe(originalServiceOwnerContextRevision);
    }
}
