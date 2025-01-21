using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogTests : ApplicationCollectionFixture
{
    public GetDialogTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Get_ReturnsSimpleDialog_WhenDialogExists()
    {
        // Arrange
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: expectedDialogId);
        var createCommandResponse = await Application.Send(createDialogCommand);

        // Act
        var response = await Application.Send(new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(createDialogCommand.Dto, options => options
            .Excluding(x => x.UpdatedAt)
            .Excluding(x => x.CreatedAt)
            .Excluding(x => x.SystemLabel));
    }

    [Fact]
    public async Task Get_ReturnsDialog_WhenDialogExists()
    {
        // Arrange
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();
        var createCommand = DialogGenerator.GenerateFakeCreateDialogCommand(id: expectedDialogId);
        var createCommandResponse = await Application.Send(createCommand);

        // Act
        var response = await Application.Send(new GetDialogQuery { DialogId = createCommandResponse.AsT0.DialogId });

        // Assert
        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(createCommand.Dto, options => options
            .Excluding(x => x.UpdatedAt)
            .Excluding(x => x.CreatedAt)
            .Excluding(x => x.SystemLabel));
    }
    // TODO: Add tests
}
