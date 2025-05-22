using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.Commands.Update;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateDialogServiceOwnerLabelTests : ApplicationCollectionFixture
{
    public UpdateDialogServiceOwnerLabelTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Cannot_Call_Update_ServiceOwnerLabels_Without_DialogId_Or_Dto()
    {
        // Arrange
        var setServiceOwnerLabelsCommand = new UpdateDialogServiceOwnerContextCommand();

        // Act
        var response = await Application.Send(setServiceOwnerLabelsCommand);

        // Assert
        response.TryPickT1(out var validationError, out _).Should().BeTrue();
        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(nameof(UpdateDialogServiceOwnerContextCommand.DialogId)));

        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains(nameof(UpdateDialogServiceOwnerContextCommand.Dto)));
    }

    [Fact]
    public async Task Calling_UpdateDialogServiceOwnerContext_With_Invalid_DialogId_Returns_NotFound()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var setServiceOwnerLabelsCommand = new UpdateDialogServiceOwnerContextCommand
        {
            DialogId = dialogId,
            Dto = new()
        };

        // Act
        var response = await Application.Send(setServiceOwnerLabelsCommand);

        // Assert
        response.TryPickT2(out var notFoundError, out _).Should().BeTrue();
        notFoundError.Should().NotBeNull();
        notFoundError.Message.Should().Contain(dialogId.ToString());
    }

    [Fact]
    public async Task Can_Remove_ServiceOwnerLabels()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        createDialogCommand.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = "Scadrial" }];
        await Application.Send(createDialogCommand);

        var updateLabelsCommand = new UpdateDialogServiceOwnerContextCommand
        {
            DialogId = dialogId,
            Dto = new()
        };

        // Act
        var response = await Application.Send(updateLabelsCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.Revision.Should().NotBe(Guid.Empty);

        await Application.AssertEntityCountAsync<DialogServiceOwnerLabel>(count: 0);
    }

    [Fact]
    public async Task Can_Add_ServiceOwnerLabel_To_Existing_Dialog()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        await Application.Send(createDialogCommand);

        var updateLabelsCommand = new UpdateDialogServiceOwnerContextCommand
        {
            DialogId = dialogId,
            Dto = new()
            {
                ServiceOwnerLabels =
                [
                    new() { Value = "Scadrial" },
                    new() { Value = "Roshar" },
                    new() { Value = "Sel" }
                ]
            }
        };

        // Act
        var response = await Application.Send(updateLabelsCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.Revision.Should().NotBe(Guid.Empty);

        var dialogResponse = await Application.Send(new GetDialogQuery() { DialogId = dialogId });
        dialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();
        dialog.Should().NotBeNull();

        await Application.AssertEntityCountAsync<DialogServiceOwnerLabel>(count: 3);
    }

    [Fact]
    public async Task Cannot_Update_ServiceOwnerLabels_With_Duplicates()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        await Application.Send(createDialogCommand);

        const string label = "SCADRIAL";

        var setServiceOwnerLabelsCommand = new UpdateDialogServiceOwnerContextCommand
        {
            DialogId = dialogId,
            Dto = new()
            {
                ServiceOwnerLabels =
                [
                    new() { Value = label },
                    new() { Value = label.ToLowerInvariant() }
                ]
            }
        };

        // Act
        var response = await Application.Send(setServiceOwnerLabelsCommand);

        // Assert
        response.TryPickT1(out var validationError, out _).Should().BeTrue();
        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("duplicate"));
    }

    [Fact]
    public async Task Cannot_Update_ServiceOwnerLabels_With_Invalid_Length()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        await Application.Send(createDialogCommand);

        var setServiceOwnerLabelsCommand = new UpdateDialogServiceOwnerContextCommand
        {
            DialogId = dialogId,
            Dto = new()
            {
                ServiceOwnerLabels =
                [
                    new() { Value = null! },
                    new() { Value = new string('a', Constants.MinSearchStringLength - 1) },
                    new() { Value = new string('a', Constants.DefaultMaxStringLength + 1) }
                ]
            }
        };

        // Act
        var response = await Application.Send(setServiceOwnerLabelsCommand);

        // Assert
        response.TryPickT1(out var validationError, out _).Should().BeTrue();

        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("not be empty"));

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
    public async Task Update_ServiceOwnerLabels_Should_Update_ServiceOwnerContext_Revision()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        await Application.Send(createDialogCommand);

        var originalServiceOwnerContextRevision =
            (await Application.GetDbEntities<DialogServiceOwnerContext>())
            .Single(x => x.DialogId == dialogId).Revision;

        var setServiceOwnerLabelsCommand = new UpdateDialogServiceOwnerContextCommand
        {
            DialogId = dialogId,
            Dto = new()
            {
                ServiceOwnerLabels =
                [
                    new() { Value = "Scadrial" }
                ]
            }
        };

        // Act
        var response = await Application.Send(setServiceOwnerLabelsCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();

        originalServiceOwnerContextRevision.Should().NotBe(Guid.Empty);
        success.Revision.Should().NotBe(originalServiceOwnerContextRevision);
    }

    [Fact]
    public async Task Cannot_Update_With_More_Than_Max_Allowed_ServiceOwner_Labels()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        await Application.Send(createDialogCommand);

        var setServiceOwnerLabelsCommand = new UpdateDialogServiceOwnerContextCommand
        {
            DialogId = dialogId,
            Dto = new()
        };

        Enumerable.Range(0, DialogServiceOwnerLabel.MaxNumberOfLabels + 1).ToList()
            .ForEach(i => setServiceOwnerLabelsCommand
                .Dto.ServiceOwnerLabels.Add(new() { Value = $"label{i}" }));

        // Act
        var response = await Application.Send(setServiceOwnerLabelsCommand);

        // Assert
        response.TryPickT1(out var validationError, out _).Should().BeTrue();
        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("Maximum") && x.ErrorMessage
                .Contains($"{DialogServiceOwnerLabel.MaxNumberOfLabels}"));
    }
}
