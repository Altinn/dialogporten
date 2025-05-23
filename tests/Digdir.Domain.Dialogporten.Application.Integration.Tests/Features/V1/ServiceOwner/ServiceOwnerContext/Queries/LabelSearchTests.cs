using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchServiceOwnerLabelTests : ApplicationCollectionFixture
{
    public SearchServiceOwnerLabelTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Can_Filter_On_Single_Label()
    {
        // Arrange
        var createDialogWithoutLabel = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        await Application.Send(createDialogWithoutLabel);

        const string label = "Scadrial";

        var labeledDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogWithLabel = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: labeledDialogId);
        createDialogWithLabel.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = label }];
        await Application.Send(createDialogWithLabel);

        var searchServiceOwnerLabelQuery = new SearchDialogQuery { ServiceOwnerLabels = [label] };

        // Act
        var response = await Application.Send(searchServiceOwnerLabelQuery);

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(labeledDialogId);

        await Application.AssertEntityCountAsync<DialogEntity>(count: 2);
    }

    [Fact]
    public async Task Multiple_Label_Inputs_Must_All_Match()
    {
        // Arrange
        var createDialogWithoutLabel = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        await Application.Send(createDialogWithoutLabel);

        const string label1 = "Scadrial";
        const string label2 = "Roshar";

        var createDialogWithOneLabel = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogWithOneLabel.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = label1 }];
        await Application.Send(createDialogWithOneLabel);

        var dualLabeledDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogWithTwoLabels = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dualLabeledDialogId);
        createDialogWithTwoLabels.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = label1 }, new() { Value = label2 }];
        await Application.Send(createDialogWithTwoLabels);

        var searchServiceOwnerLabelQuery = new SearchDialogQuery { ServiceOwnerLabels = [label1, label2] };

        // Act
        var response = await Application.Send(searchServiceOwnerLabelQuery);

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(dualLabeledDialogId);

        await Application.AssertEntityCountAsync<DialogEntity>(count: 3);
    }

    [Fact]
    public async Task Can_Filter_On_Label_Prefix()
    {
        // Arrange
        const int numberOfDialogs = 30;
        for (var i = 0; i < numberOfDialogs; i++)
        {
            var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
            await Application.Send(createDialogCommand);
        }

        const string label1 = "ScadrialOne";
        const string label2 = "ScadrialTwo";

        var singleLabeledDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogWithOneLabel = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: singleLabeledDialogId);
        createDialogWithOneLabel.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = label1 }];
        await Application.Send(createDialogWithOneLabel);

        var dualLabeledDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogWithTwoLabels = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dualLabeledDialogId);
        createDialogWithTwoLabels.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = label2 }];
        await Application.Send(createDialogWithTwoLabels);

        var searchServiceOwnerLabelQuery = new SearchDialogQuery { ServiceOwnerLabels = [label1[..4] + "*"] };

        // Act
        var response = await Application.Send(searchServiceOwnerLabelQuery);

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Items.Should().HaveCount(2);
        result.Items.Should().ContainSingle(x => x.Id == singleLabeledDialogId);
        result.Items.Should().ContainSingle(x => x.Id == dualLabeledDialogId);

        await Application.AssertEntityCountAsync<DialogEntity>(count: numberOfDialogs + 2);
    }

    [Fact]
    public async Task Filtering_On_Non_Existing_Label_Returns_No_Results()
    {
        // Arrange
        var createDialogWithoutLabel = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        await Application.Send(createDialogWithoutLabel);

        const string label1 = "ScadrialOne";
        const string label2 = "ScadrialTwo";

        var singleLabeledDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogWithOneLabel = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: singleLabeledDialogId);
        createDialogWithOneLabel.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = label1 }];
        await Application.Send(createDialogWithOneLabel);

        var dualLabeledDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogWithTwoLabels = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dualLabeledDialogId);
        createDialogWithTwoLabels.Dto.ServiceOwnerContext!.ServiceOwnerLabels = [new() { Value = label2 }];
        await Application.Send(createDialogWithTwoLabels);

        var searchServiceOwnerLabelQuery = new SearchDialogQuery { ServiceOwnerLabels = ["One"] };

        // Act
        var response = await Application.Send(searchServiceOwnerLabelQuery);

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Items.Should().HaveCount(0);

        await Application.AssertEntityCountAsync<DialogEntity>(count: 3);
    }

    [Fact]
    public async Task Cannot_Filter_On_Invalid_Label_Length()
    {
        // Arrange
        var searchServiceOwnerLabelQuery = new SearchDialogQuery
        {
            ServiceOwnerLabels = [
                new string('a', Constants.MinSearchStringLength - 1),
                new string('a', Constants.DefaultMaxStringLength + 1)
            ]
        };

        // Act
        var response = await Application.Send(searchServiceOwnerLabelQuery);

        // Assert
        response.TryPickT1(out var validationError, out _).Should().BeTrue();
        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("at least"));

        validationError.Errors
            .Should()
            .ContainSingle(x => x.ErrorMessage
                .Contains("or fewer"));
    }
}
