using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.ServiceOwnerContext.ServiceOwnerLabels.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using ServiceOwnerLabelDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.ServiceOwnerLabelDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.ServiceOwnerContext.Queries;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetServiceOwnerLabelsTest : ApplicationCollectionFixture
{
    public GetServiceOwnerLabelsTest(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Can_Get_ServiceOwnerLabels()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        var labels = CreateLabels("Scadrial", "Roshar", "Sel");
        createDialogCommand.Dto.ServiceOwnerContext!.ServiceOwnerLabels = labels;

        await Application.Send(createDialogCommand);

        // Act
        var getServiceOwnerLabelsQuery = new GetServiceOwnerLabelsQuery
        {
            DialogId = dialogId
        };

        // Act
        var response = await Application.Send(getServiceOwnerLabelsQuery);
        response.TryPickT0(out var serviceOwnerLabels, out _).Should().BeTrue();
        serviceOwnerLabels.Should().NotBeNull();
        serviceOwnerLabels.Labels.Should().HaveCount(labels.Count);
    }

    [Fact]
    public async Task Get_ServiceOwnerLabels_With_Invalid_DialogId_Returns_NotFound()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var getServiceOwnerLabelsQuery = new GetServiceOwnerLabelsQuery
        {
            DialogId = dialogId
        };

        // Act
        var response = await Application.Send(getServiceOwnerLabelsQuery);

        // Assert
        response.TryPickT1(out var notFoundError, out _).Should().Be(true);
        notFoundError.Should().NotBeNull();
        notFoundError.Message.Should().Contain(dialogId.ToString());
    }

    private static List<ServiceOwnerLabelDto> CreateLabels(params string[] labels) =>
        labels.Select(label => new ServiceOwnerLabelDto { Value = label }).ToList();
}
