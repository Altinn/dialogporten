using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using ServiceOwnerLabelDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ServiceOwnerLabelDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateDialogServiceOwnerLabelTests : ApplicationCollectionFixture
{
    public CreateDialogServiceOwnerLabelTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Can_Create_Dialog_With_ServiceOwner_Labels()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        var labels = new List<ServiceOwnerLabelDto>
        {
            new() { Value = "Scadrial" },
            new() { Value = "Roshar" },
            new() { Value = "Sel" }
        };

        createDialogCommand.Dto.ServiceOwnerContext!.ServiceOwnerLabels = labels;

        // Act
        var response = await Application.Send(createDialogCommand);

        var getDialogResponse = await Application.Send(new GetDialogQuery
        {
            DialogId = dialogId
        });

        // Assert
        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();
        dialog.Should().NotBeNull();
        dialog.ServiceOwnerContext.ServiceOwnerLabels.Should().HaveCount(labels.Count);

        response.TryPickT0(out _, out _).Should().BeTrue();
        await Application.AssertEntityCountAsync<DialogServiceOwnerLabel>(count: labels.Count);
    }

    [Fact]
    public async Task Cannot_Create_Dialog_With_Duplicate_ServiceOwner_Labels()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        const string label = "SCADRIAL";

        List<ServiceOwnerLabelDto> labels =
        [
            new() { Value = label },
            new() { Value = label.ToLowerInvariant() }
        ];

        createDialogCommand.Dto.ServiceOwnerContext!.ServiceOwnerLabels = labels;

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("duplicate"));
    }

    [Fact]
    public async Task Cannot_Create_Dialog_With_Invalid_Length()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var labels = new List<ServiceOwnerLabelDto>
        {
            new() { Value = null! },
            new() { Value = new string('a', Constants.MinSearchStringLength - 1) },
            new() { Value = new string('a', Constants.DefaultMaxStringLength + 1) }
        };

        createDialogCommand.Dto.ServiceOwnerContext!.ServiceOwnerLabels = labels;

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();

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
    public async Task Cannot_Create_More_Than_Maximum_ServiceOwner_Labels()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var labels = new List<ServiceOwnerLabelDto>();

        for (var i = 0; i < DialogServiceOwnerLabel.MaxNumberOfLabels + 1; i++)
        {
            labels.Add(new ServiceOwnerLabelDto { Value = $"label{i}" });
        }

        createDialogCommand.Dto.ServiceOwnerContext!.ServiceOwnerLabels = labels;

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("Maximum") && x.ErrorMessage
                .Contains($"{DialogServiceOwnerLabel.MaxNumberOfLabels}"));
    }
}
