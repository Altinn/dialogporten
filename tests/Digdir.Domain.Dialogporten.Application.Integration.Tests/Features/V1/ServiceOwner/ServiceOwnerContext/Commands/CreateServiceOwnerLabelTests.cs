using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.ServiceOwnerContexts.Entities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateServiceOwnerLabelTests : ApplicationCollectionFixture
{
    public CreateServiceOwnerLabelTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Can_Create_Dialog_With_ServiceOwner_Labels()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var labels = new List<ServiceOwnerLabelDto>
        {
            new() { Value = "Scadrial" },
            new() { Value = "Roshar" },
            new() { Value = "Sel" }
        };

        createDialogCommand.Dto.ServiceOwnerLabels = labels;

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT0(out _, out _).Should().BeTrue();
        var serviceOwnerLabels = await Application.GetDbEntities<ServiceOwnerLabel>();
        serviceOwnerLabels.Should().HaveCount(labels.Count);
    }

    [Fact]
    public async Task Cannot_Create_Dialog_With_Duplicate_ServiceOwner_Labels()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var labels = new List<ServiceOwnerLabelDto>
        {
            new() { Value = "Scadrial" },
            new() { Value = "Scadrial" },
        };

        createDialogCommand.Dto.ServiceOwnerLabels = labels;

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
            new() { Value = new string('a', Constants.MinSearchStringLength - 1) },
            new() { Value = new string('a', Constants.DefaultMaxStringLength + 1) }
        };

        createDialogCommand.Dto.ServiceOwnerLabels = labels;

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
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

        for (var i = 0; i < ServiceOwnerLabel.MaxNumberOfLabels + 1; i++)
        {
            labels.Add(new ServiceOwnerLabelDto { Value = $"label{i}" });
        }

        createDialogCommand.Dto.ServiceOwnerLabels = labels;

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("Maximum") && x.ErrorMessage
                .Contains($"{ServiceOwnerLabel.MaxNumberOfLabels}"));
    }
}
